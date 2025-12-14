using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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

// 定义 .NET Framework 版本枚举
public enum DotNetFrameworkVersion
{
    Net60,     // .NET 6.0
    Net70,     // .NET 7.0
    Net80,     // .NET 8.0
    Net90,     // .NET 9.0
    Net100,    // .NET 10.0
    NetStandard20, // .NET Standard 2.0
    NetStandard21, // .NET Standard 2.1
}

// 定义 C# 语言版本枚举
public enum CSharpLanguageVersion
{
    Latest,      // 最新版本
    Preview,     // 预览版本
    CSharp3,     // C# 3.0
    CSharp4,     // C# 4.0
    CSharp5,     // C# 5.0
    CSharp6,     // C# 6.0
    CSharp7,     // C# 7.0
    CSharp8,     // C# 8.0
    CSharp9,     // C# 9.0
    CSharp10,    // C# 10.0
    CSharp11,    // C# 11.0
    CSharp12,    // C# 12.0
    CSharp13,    // C# 13.0
}

public class DotNetProjectCreator
{
    private static ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<DotNetProjectCreator>();

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
    /// <param name="framework">目标框架版本</param>
    /// <param name="additionalArgs">额外的参数数组，例如 ["-n", "MyProjectName"]</param>
    /// <returns>是否成功执行</returns>
    public static bool CreateProject(
        ProjectTemplate template,
        string outputDirectory,
        DotNetFrameworkVersion framework,
        params string[] additionalArgs
    )
    {
        // 将枚举转换为字符串名称
        string templateName = Enum.GetName(typeof(ProjectTemplate), template).ToLowerInvariant();
        string frameworkName = GetFrameworkVersionString(framework);

        // 构建参数
        var argsList = new List<string> { $"new {templateName}", $"-o \"{outputDirectory}\"", $"-f {frameworkName}" };
        if (additionalArgs != null && additionalArgs.Length > 0)
        {
            argsList.AddRange(additionalArgs);
        }

        // 配置 ProcessStartInfo
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", argsList),
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
                    logger.LogError("Failed to start 'dotnet' process.");
                    return false;
                }

                // 订阅输出和错误事件以实现流式处理
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogInformation("[STDOUT] {Data}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogError("[STDERR] {Data}", e.Data);
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
                    logger.LogInformation("Project created successfully!");
                    return true;
                }
                else
                {
                    logger.LogError("dotnet new failed with exit code: {ExitCode}", exitCode);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running 'dotnet new'");
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
                    logger.LogError("Failed to start 'dotnet' process.");
                    return false;
                }

                // 订阅输出和错误事件以实现流式处理
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogInformation("[STDOUT] {Data}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogError("[STDERR] {Data}", e.Data);
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
                    logger.LogInformation("Project created successfully!");
                    return true;
                }
                else
                {
                    logger.LogError("dotnet new failed with exit code: {ExitCode}", exitCode);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running 'dotnet new'");
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
                    logger.LogError("Failed to start 'dotnet build' process.");
                    return false;
                }

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogInformation("[BUILD STDOUT] {Data}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogError("[BUILD STDERR] {Data}", e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                int exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    logger.LogInformation("[BUILD] Project built successfully!");
                    return true;
                }
                else
                {
                    logger.LogError("[BUILD] dotnet build failed with exit code: {ExitCode}", exitCode);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[BUILD] An error occurred while running 'dotnet build'");
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
                    logger.LogError("Failed to start 'dotnet run' process.");
                    return false;
                }

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogInformation("[RUN STDOUT] {Data}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogError("[RUN STDERR] {Data}", e.Data);
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
                    logger.LogInformation("[RUN] Project executed successfully!");
                    return true;
                }
                else
                {
                    logger.LogError("[RUN] Project execution failed with exit code: {ExitCode}", exitCode);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[RUN] An error occurred while running 'dotnet run'");
            return false;
        }
    }

    /// <summary>
    /// 创建一个新的解决方案文件
    /// </summary>
    /// <param name="solutionPath">解决方案文件的完整路径（包括.sln扩展名）</param>
    /// <returns>是否成功创建</returns>
    public static bool CreateSolution(string solutionPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new sln -n \"{Path.GetFileNameWithoutExtension(solutionPath)}\" -o \"{Path.GetDirectoryName(solutionPath)}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default,
            WorkingDirectory = Path.GetDirectoryName(solutionPath)
        };

        try
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    logger.LogError("Failed to start 'dotnet new sln' process.");
                    return false;
                }

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogInformation("[SLN STDOUT] {Data}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogError("[SLN STDERR] {Data}", e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                int exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    logger.LogInformation("Solution created successfully at: {SolutionPath}", solutionPath);
                    return true;
                }
                else
                {
                    logger.LogError("dotnet new sln failed with exit code: {ExitCode}", exitCode);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running 'dotnet new sln'");
            return false;
        }
    }

    /// <summary>
    /// 将项目添加到解决方案中
    /// </summary>
    /// <param name="solutionPath">解决方案文件的路径</param>
    /// <param name="projectPath">要添加的项目文件路径</param>
    /// <returns>是否成功添加</returns>
    public static bool AddProjectToSolution(string solutionPath, string projectPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"sln \"{solutionPath}\" add \"{projectPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default,
            WorkingDirectory = Path.GetDirectoryName(solutionPath)
        };

        try
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    logger.LogError("Failed to start 'dotnet sln add' process.");
                    return false;
                }

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogInformation("[SLN ADD STDOUT] {Data}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        logger.LogError("[SLN ADD STDERR] {Data}", e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                int exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    logger.LogInformation("Project added successfully to solution: {SolutionPath}", solutionPath);
                    return true;
                }
                else
                {
                    logger.LogError("dotnet sln add failed with exit code: {ExitCode}", exitCode);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running 'dotnet sln add'");
            return false;
        }
    }

    /// <summary>
    /// 将 DotNetFrameworkVersion 枚举转换为对应的字符串
    /// </summary>
    /// <param name="version">框架版本枚举</param>
    /// <returns>对应的字符串表示</returns>
    private static string GetFrameworkVersionString(DotNetFrameworkVersion version)
    {
        return version switch
        {
            DotNetFrameworkVersion.Net60 => "net6.0",
            DotNetFrameworkVersion.Net70 => "net7.0",
            DotNetFrameworkVersion.Net80 => "net8.0",
            DotNetFrameworkVersion.Net90 => "net9.0",
            DotNetFrameworkVersion.Net100 => "net10.0",
            DotNetFrameworkVersion.NetStandard20 => "netstandard2.0",
            DotNetFrameworkVersion.NetStandard21 => "netstandard2.1",
            _ => "net8.0" // 默认值
        };
    }
}