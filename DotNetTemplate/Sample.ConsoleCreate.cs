using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DotNetTemplate;

public class Sample_ConsoleCreate
{
    private static ILogger logger = LoggerFactory
        .Create(builder => builder.AddConsole())
        .CreateLogger<Sample_ConsoleCreate>();

    public void Foo()
    {
        string solutionName = @"testcmd";
        string projectName = @"testcmd";

        // 获取当前工作目录
        string workingDirectory = @"C:\Users\Administrator\Desktop\dotnetclitest";
        Directory.Delete(workingDirectory, true);
        // 解决方案目录
        string solutionDirectory = Path.Combine(workingDirectory, solutionName);
        // 项目目录
        string projectDirectory = Path.Combine(solutionDirectory, projectName);

        // 1. 创建解决方案目录
        Directory.CreateDirectory(solutionDirectory);
        logger.LogInformation("Created solution directory: {SolutionDirectory}", solutionDirectory);

        // 2. 创建解决方案文件
        string solutionPath = Path.Combine(solutionDirectory, $"{solutionName}.sln");
        DotNetProjectCreator.CreateSolution(solutionPath);

        // 3. 创建控制台项目
        DotNetProjectCreator.CreateProject(
            ProjectTemplate.Console, // 使用控制台模板
            projectDirectory, // 项目目录
            DotNetFrameworkVersion.Net80, // 指定框架版本
            "-n",
            projectName // 指定项目名称
        );

        // 4. 将项目添加到解决方案
        string projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
        DotNetProjectCreator.AddProjectToSolution(solutionPath, projectPath);

        // 5. 设置 C# 语言版本为最新版本
        CsProjModifier.SetLanguageVersion(projectPath, CSharpLanguageVersion.Latest);

        // 6. 添加 NuGet 包引用示例
        CsProjModifier.AddNuGetPackage(projectPath, "Newtonsoft.Json", "13.0.3");

        // 7. 设置 NuGet 包元数据示例
        CsProjModifier.SetNuGetPackageMetadata(
            projectPath,
            "Newtonsoft.Json",
            excludeAssets: "runtime",
            privateAssets: "all"
        );

        // 8. 添加 DLL 引用示例 (假设有 DLL 文件)
        // CsProjModifier.AddDllReferences(projectPath, true, @"C:\path\to\your\library.dll");

        logger.LogInformation("All operations completed successfully!");
        DotNetProjectCreator.BuildProject(Path.Combine(projectDirectory, projectName + ".csproj"));
        DotNetProjectCreator.RunProject(Path.Combine(projectDirectory, projectName + ".csproj"));
    }
}
