using SharpBoxesCore.Helpers;

namespace DotNetTemplate;

public class Sample_WpfCreate
{
    public void Foo()
    {
        ProjectTemplate selectedTemplate = ProjectTemplate.Wpf;
        string outputDir = Environment
            .GetFolderPath(System.Environment.SpecialFolder.Desktop)
            .PathCombine("testcCmd");
        string projectPath = Path.Combine(outputDir, "testcCmd.csproj");
        Directory.Delete(outputDir, true);
        Directory.CreateDirectory(outputDir);
        string[] extraArgs = { "-f", "net8.0" };

        bool success = DotNetProjectCreator.CreateProject(selectedTemplate, outputDir, extraArgs);

        if (success)
        {
            Console.WriteLine(
                $"\nSuccessfully created project '{selectedTemplate}' at: {outputDir}"
            );
        }
        else
        {
            Console.WriteLine(
                $"\nFailed to create project '{selectedTemplate}'. Check the logs above."
            );
        }

        // 2. 编译项目
        bool buildSuccess = DotNetProjectCreator.BuildProject(
            projectPath,
            "--configuration",
            "Debug"
        ); // 或 Release

        if (!buildSuccess)
        {
            Console.WriteLine("Failed to build project, exiting.");
            return;
        }

        // 3. 运行项目
        // 注意：如果项目需要参数，可以在这里添加
        bool runSuccess = DotNetProjectCreator.RunProject(
            projectPath /*, "arg1", "arg2" */
        );

        if (runSuccess)
        {
            Console.WriteLine("\nAll operations completed successfully!");
        }
        else
        {
            Console.WriteLine(
                "\nProject creation/build succeeded, but running failed or returned non-zero exit code."
            );
        }
    }
}
