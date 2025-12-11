# DotNetTemplate

DotNetTemplate 是一个用于自动化创建、配置和管理 .NET 项目的工具库。它提供了一套完整的 API 来简化 .NET 项目的创建过程，支持多种项目模板类型，并允许在创建后自动进行项目配置，如添加 DLL 引用、源文件以及修改项目属性等操作。

## 功能特性

- 支持多种 .NET 项目模板（控制台应用、类库、Web API、MVC、WinForms、WPF 等）
- 自动化项目创建流程
- 实时输出项目创建、构建和运行的日志信息
- 修改项目配置文件（.csproj）：
  - 添加 DLL 引用
  - 添加源代码文件
  - 设置项目属性（如 LangVersion、AllowUnsafeBlocks 等）
- 项目构建和运行功能

## 安装要求

- .NET SDK（根据您要创建的项目类型选择相应版本）
- Windows 操作系统（当前版本针对 Windows 平台开发）

## 快速开始

### 1. 检查 .NET 是否已安装

```csharp
bool isDotNetInstalled = DotNetProjectCreator.IsDotNetInstalled();
Console.WriteLine($"DotNet is installed: {isDotNetInstalled}");
```

### 2. 创建项目

```csharp
// 选择项目模板
ProjectTemplate selectedTemplate = ProjectTemplate.Console;

// 设置输出目录
string outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MyNewProject");

// 可选的附加参数（如框架版本）
string[] extraArgs = { "-f", "net8.0" };

// 创建项目
bool success = DotNetProjectCreator.CreateProject(selectedTemplate, outputDir, extraArgs);

if (success)
{
    Console.WriteLine($"成功创建项目 '{selectedTemplate}' 在: {outputDir}");
}
else
{
    Console.WriteLine($"创建项目 '{selectedTemplate}' 失败，请检查日志。");
}
```

### 3. 修改项目配置

#### 设置项目属性

```csharp
string projectPath = Path.Combine(outputDir, "MyNewProject.csproj");

// 设置允许不安全代码块
CsProjModifier.SetProperty(projectPath, "AllowUnsafeBlocks", "true");

// 设置语言版本
CsProjModifier.SetProperty(projectPath, "LangVersion", "preview");
```

#### 添加 DLL 引用

```csharp
string[] dllPathsToAdd = {
    @"C:\path\to\your\library\Newtonsoft.Json.dll"
};

CsProjModifier.AddDllReferences(projectPath, true, dllPathsToAdd);
```

#### 添加源代码文件

```csharp
string[] sourceFilesToAdd = {
    @"C:\path\to\your\Program.cs",
    @"C:\path\to\your\Function.cs"
};

CsProjModifier.AddSourceFiles(projectPath, overwriteExisting: true, sourceFilesToAdd);
```

### 4. 构建和运行项目

```csharp
// 构建项目
bool buildSuccess = DotNetProjectCreator.BuildProject(
    projectPath,
    "--configuration",
    "Debug"
);

if (!buildSuccess)
{
    Console.WriteLine("构建项目失败，正在退出。");
    return;
}

// 运行项目
bool runSuccess = DotNetProjectCreator.RunProject(projectPath);

if (runSuccess)
{
    Console.WriteLine("所有操作成功完成！");
}
else
{
    Console.WriteLine("项目创建/构建成功，但运行失败或返回了非零退出码。");
}
```

## 示例代码

项目包含两个完整的示例，演示如何使用该库：

1. [Sample.ConsoleCreate](DotNetTemplate/Sample.ConsoleCreate.cs) - 控制台应用创建和配置示例
2. [Sample.WpfCreate](DotNetTemplate/Sample.WpfCreate.cs) - WPF 应用创建示例

这些示例展示了从项目创建到配置再到构建和运行的完整流程。

## API 文档

### ProjectTemplate 枚举

定义了可用的 .NET 项目模板：

- `Console` - 控制台应用
- `ClassLib` - 类库
- `WebApi` - Web API 应用
- `Mvc` - MVC Web 应用
- `WinForms` - Windows Forms 应用
- `Wpf` - WPF 应用
- `Worker` - 后台服务
- `XUnit` - xUnit 测试项目
- `NUnit` - NUnit 测试项目
- `MsTest` - MSTest 测试项目
- `RazorClassLibrary` - Razor 类库

### DotNetProjectCreator 类

#### IsDotNetInstalled()

检查系统是否安装了 .NET SDK。

#### CreateProject(ProjectTemplate template, string outputDirectory, params string[] additionalArgs)

使用指定模板创建新的 .NET 项目。

#### BuildProject(string projectPath, params string[] additionalArgs)

编译指定的 .NET 项目。

#### RunProject(string projectPath, params string[] additionalArgs)

运行指定的 .NET 项目。

### CsProjModifier 类

#### SetProperty(string csprojFilePath, string propertyName, string propertyValue)

设置或更新 .csproj 文件中的属性。

#### AddDllReferences(string csprojFilePath, bool copyToLocal, params string[] dllPaths)

向 .csproj 文件添加 DLL 引用。

#### AddSourceFiles(string csprojFilePath, bool overwriteExisting, params string[] sourceFilePaths)

向 .csproj 文件添加源代码文件。

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE.txt](LICENSE.txt) 获取详细信息。