using System.Diagnostics;
using System.Text;

namespace DotNetTemplate;

// 1. 定义 .NET 模板枚举
public enum ProjectTemplate
{
    Console, // 控制台应用
    ClassLib, // 类库
    WebApi, // Web API
    Mvc, // MVC Web 应用
    WinForms, // Windows Forms 应用
    Wpf, // WPF 应用
    Worker, // 后台服务
    XUnit, // xUnit 测试项目
    NUnit, // NUnit 测试项目
    MsTest, // MSTest 测试项目
    RazorClassLibrary, // Razor 类库
    // 可以根据需要添加更多模板
}

public class DotNetProjectCreator
{
    public static bool IsDotNetInstalled()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version", // 或者 "--help"
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null) return false;

                process.WaitForExit();
                return process.ExitCode == 0; // 如果命令成功执行，说明已安装
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // 如果抛出 Win32Exception (例如 "系统找不到指定的文件")，说明 dotnet 命令不存在
            return false;
        }
    }
    
    /// <summary>
    /// 使用 dotnet new 命令创建新项目，并实时输出日志
    /// </summary>
    /// <param name="template">要使用的模板</param>
    /// <param name="outputDirectory">项目输出目录</param>
    /// <param name="additionalArgs">额外的参数数组，例如 ["-f", "net8.0", "-n", "MyProjectName"]</param>
    /// <returns>是否成功执行</returns>
    public static bool CreateProject(
        ProjectTemplate template,
        string outputDirectory,
        params string[] additionalArgs
    )
    {
        // 将枚举转换为字符串名称
        string templateName = Enum.GetName(typeof(ProjectTemplate), template).ToLowerInvariant();

        // 配置 ProcessStartInfo
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments =
                $"new {templateName} -o \"{outputDirectory}\" " + string.Join(" ", additionalArgs),
            UseShellExecute = false,
            RedirectStandardOutput = true, // 重定向输出
            RedirectStandardError = true, // 重定向错误
            CreateNoWindow = true,
            // 2. 关键：设置正确的编码
            StandardOutputEncoding = Encoding.Default, // 使用系统默认编码 (通常是GBK/GB2312)
            StandardErrorEncoding = Encoding.Default, // 使用系统默认编码
            // WorkingDirectory = outputParentDir // 可选设置工作目录
        };

        try
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("Failed to start 'dotnet' process.");
                    return false;
                }

                // 订阅输出和错误事件以实现流式处理
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[STDOUT] {e.Data}"); // 实时打印标准输出 (现在应该正常)
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[STDERR] {e.Data}"); // 实时打印标准错误 (现在应该正常)
                    }
                };

                // 开始异步读取输出和错误流
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 等待进程退出
                process.WaitForExit();

                int exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    Console.WriteLine("Project created successfully!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"dotnet new failed with exit code: {exitCode}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while running 'dotnet new': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 编译指定的 .NET 项目，并实时输出日志
    /// </summary>
    /// <param name="projectPath">项目文件 (.csproj/.fsproj/.vbproj) 或项目目录的路径</param>
    /// <param name="additionalArgs">额外的参数数组，例如 ["--configuration", "Release"]</param>
    /// <returns>是否成功执行</returns>
    public static bool BuildProject(string projectPath, params string[] additionalArgs)
    {
        // 配置 ProcessStartInfo
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" " + string.Join(" ", additionalArgs),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default,
            WorkingDirectory = Path.GetDirectoryName(projectPath), // 设置工作目录为项目目录或其父目录
        };

        try
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("Failed to start 'dotnet build' process.");
                    return false;
                }

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[BUILD STDOUT] {e.Data}"); // 区分编译日志
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[BUILD STDERR] {e.Data}"); // 区分编译日志
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                int exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    Console.WriteLine("[BUILD] Project built successfully!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[BUILD] dotnet build failed with exit code: {exitCode}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[BUILD] An error occurred while running 'dotnet build': {ex.Message}"
            );
            return false;
        }
    }

    /// <summary>
    /// 运行指定的 .NET 项目（通常是可执行项目），并实时输出日志
    /// </summary>
    /// <param name="projectPath">项目文件 (.csproj/.fsproj/.vbproj) 或项目目录的路径</param>
    /// <param name="additionalArgs">传递给应用程序的参数数组，例如 ["arg1", "arg2"]</param>
    /// <returns>是否成功执行（注意：如果应用程序本身返回非零退出码，这里也会返回 false）</returns>
    public static bool RunProject(string projectPath, params string[] additionalArgs)
    {
        // 配置 ProcessStartInfo
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" " + string.Join(" ", additionalArgs), // 使用 --project 参数指定运行哪个项目
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default,
            WorkingDirectory = Path.GetDirectoryName(projectPath), // 设置工作目录为项目目录或其父目录
        };

        try
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("Failed to start 'dotnet run' process.");
                    return false;
                }

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[RUN STDOUT] {e.Data}"); // 区分运行日志
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[RUN STDERR] {e.Data}"); // 区分运行日志
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                int exitCode = process.ExitCode;

                // 注意：这里 exitCode 0 代表应用程序本身成功执行（返回 0）
                // 如果应用程序返回非零值，这里也认为是 "失败"（虽然有时非零退出码是预期的行为）
                if (exitCode == 0)
                {
                    Console.WriteLine("[RUN] Project executed successfully!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[RUN] Project execution failed with exit code: {exitCode}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RUN] An error occurred while running 'dotnet run': {ex.Message}");
            return false;
        }
    }
}