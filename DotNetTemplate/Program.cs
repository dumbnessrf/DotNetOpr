using DotNetTemplate;
using SharpBoxesCore.Helpers;

Console.WriteLine($"Please See example code in [{nameof(Sample_ConsoleCreate)}] and [{nameof(Sample_WpfCreate)}] ");
new Sample_ConsoleCreate().Foo();
// 示例：如何在创建项目时一起创建解决方案
/*
 * 新增功能说明：
 * 1. DotNetProjectCreator.CreateSolution(string solutionPath) - 创建新的解决方案文件
 * 2. DotNetProjectCreator.AddProjectToSolution(string solutionPath, string projectPath) - 将项目添加到解决方案
 *
 * 使用步骤：
 * 1. 先调用 CreateSolution 方法创建解决方案
 * 2. 再调用 CreateProject 方法创建项目
 * 3. 最后调用 AddProjectToSolution 将项目添加到解决方案中
 *
 * 详细示例请参考 Sample_ConsoleCreate.cs 和 Sample_WpfCreate.cs 文件中的 Foo() 方法实现
 */

// 示例：如何使用枚举参数
/*
 * 新增枚举类型：
 * 1. DotNetFrameworkVersion - 用于指定 .NET 框架版本
 * 2. CSharpLanguageVersion - 用于指定 C# 语言版本
 *
 * 使用方法：
 * 1. 在 CreateProject 方法中使用 DotNetFrameworkVersion 枚举替代手动输入框架字符串
 * 2. 使用 CsProjModifier.SetLanguageVersion 方法配合 CSharpLanguageVersion 枚举设置语言版本
 *
 * 详细示例请参考 Sample_ConsoleCreate.cs 和 Sample_WpfCreate.cs 文件中的 Foo() 方法实现
 */