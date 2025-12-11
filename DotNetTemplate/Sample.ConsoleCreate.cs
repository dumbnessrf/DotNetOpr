using SharpBoxesCore.Helpers;

namespace DotNetTemplate;

public class Sample_ConsoleCreate
{
    public void Foo()
    {
        #region Prequisites

        string programCode = """
            using Newtonsoft.Json;
            var res = Function.Add(10,20);
            Console.WriteLine(res);
            Console.WriteLine("Hello World!");
            var obj = new {Name="John Doe", Age=30};
            Console.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
            """;
        string functionCode = """
            public static class Function
            {
                public static int Add(int a, int b)
                {
                    return a + b;
                }
            }
            """;
        var programCodePath = Environment
            .GetFolderPath(Environment.SpecialFolder.Desktop)
            .PathCombine("Program.cs");
        File.WriteAllText(programCodePath, programCode);
        var functionCodePath = Environment
            .GetFolderPath(Environment.SpecialFolder.Desktop)
            .PathCombine("Function.cs");
        File.WriteAllText(functionCodePath, functionCode);
        string[] dllPathsToAdd =
        [
            @"C:\Users\Administrator\Desktop\DotNetOpr\ProjectTemplate\bin\Debug\net10.0\Newtonsoft.Json.dll",
        ];
        string[] sourceFilesToAdd = [programCodePath, functionCodePath];

        #endregion
        Console.WriteLine($"DotNet is installed:{DotNetProjectCreator.IsDotNetInstalled()}");

        ProjectTemplate selectedTemplate = ProjectTemplate.Console;
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
        // 3. 设置其他属性，例如 AllowUnsafeBlocks
        bool unsafeBlocksSet = CsProjModifier.SetProperty(projectPath, "AllowUnsafeBlocks", "true");
        bool langVersionSet = CsProjModifier.SetProperty(projectPath, "LangVersion", "preview");

        bool modifySuccess = CsProjModifier.AddDllReferences(projectPath, true, dllPathsToAdd);

        if (!modifySuccess)
        {
            Console.WriteLine("Failed to modify .csproj file, exiting.");
            return;
        }
        bool modifySourceSuccess = CsProjModifier.AddSourceFiles(
            projectPath,
            overwriteExisting: true,
            sourceFilesToAdd
        );

        if (!modifySourceSuccess)
        {
            Console.WriteLine("Failed to modify .csproj file for source files, exiting.");
            return;
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
