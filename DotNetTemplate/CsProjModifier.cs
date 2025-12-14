using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DotNetTemplate;

public class CsProjModifier
{
    private static ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<CsProjModifier>();
    
    /// <summary>
    /// Sets or updates a property in the .csproj file.
    /// </summary>
    /// <param name="csprojFilePath">The path to the .csproj file.</param>
    /// <param name="propertyName">The name of the property to set (e.g., LangVersion, TargetFramework).</param>
    /// <param name="propertyValue">The value to assign to the property (e.g., "latest", "net8.0").</param>
    /// <returns>True if the property was set or updated, false if it was already set to the desired value or an error occurred.</returns>
    public static bool SetProperty(string csprojFilePath, string propertyName, string propertyValue)
    {
        if (!File.Exists(csprojFilePath))
        {
            logger.LogError(".csproj file does not exist: {CsprojFilePath}", csprojFilePath);
            return false;
        }

        try
        {
            var projectRoot = ProjectRootElement.Open(csprojFilePath);
            bool result = SetPropertyInternal(projectRoot, propertyName, propertyValue);

            if (result)
            {
                projectRoot.Save();
                logger.LogInformation("Set property '{PropertyName}' to '{PropertyValue}' in .csproj file: {CsprojFilePath}", propertyName, propertyValue, csprojFilePath);
            }
            else
            {
                logger.LogInformation("Property '{PropertyName}' was already set to '{PropertyValue}' or could not be modified in .csproj file: {CsprojFilePath}", propertyName, propertyValue, csprojFilePath);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to modify .csproj file '{CsprojFilePath}' for property '{PropertyName}'", csprojFilePath, propertyName);
            return false;
        }
    }
    
    /// <summary>
    /// Sets the C# language version in the .csproj file.
    /// </summary>
    /// <param name="csprojFilePath">The path to the .csproj file.</param>
    /// <param name="langVersion">The C# language version to set.</param>
    /// <returns>True if the property was set or updated, false if it was already set to the desired value or an error occurred.</returns>
    public static bool SetLanguageVersion(string csprojFilePath, CSharpLanguageVersion langVersion)
    {
        string langVersionString = GetLanguageVersionString(langVersion);
        return SetProperty(csprojFilePath, "LangVersion", langVersionString);
    }

    /// <summary>
    /// 向指定的 .csproj 文件添加一个或多个对本地 DLL 的引用。
    /// </summary>
    /// <param name="csprojFilePath">.csproj 文件的完整路径</param>
    /// <param name="dllPaths">要添加引用的 DLL 文件路径数组</param>
    /// <param name="copyToLocal">是否将 DLL 复制到输出目录 (默认 true)</param>
    /// <returns>是否成功修改并保存文件</returns>
    public static bool AddDllReferences(
        string csprojFilePath,
        bool copyToLocal = true,
        params string[] dllPaths
    )
    {
        if (!File.Exists(csprojFilePath))
        {
            logger.LogError(".csproj file does not exist: {CsprojFilePath}", csprojFilePath);
            return false;
        }

        if (dllPaths == null || dllPaths.Length == 0)
        {
            logger.LogInformation("No DLL paths provided to add.");
            return true; // Nothing to do, but not an error
        }

        try
        {
            // 1. Load the existing .csproj file
            var projectRoot = ProjectRootElement.Open(csprojFilePath);

            // 2. Find or create an ItemGroup for References
            ProjectItemGroupElement itemGroup = null;
            foreach (var ig in projectRoot.ItemGroups)
            {
                if (
                    ig.Items.Any(i =>
                        i.ElementName.Equals("Reference", StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    itemGroup = ig;
                    break;
                }
            }

            if (itemGroup == null)
            {
                // Create a new ItemGroup if none exists with References
                itemGroup = projectRoot.AddItemGroup();
            }

            bool changesMade = false;
            foreach (var dllPath in dllPaths)
            {
                if (!File.Exists(dllPath))
                {
                    logger.LogWarning("DLL file does not exist, skipping: {DllPath}", dllPath);
                    continue;
                }

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(dllPath);
                string hintPath = Path.GetFullPath(dllPath);

                // Check if the reference already exists
                var existingRef = itemGroup.Items.FirstOrDefault(i =>
                    i.ElementName.Equals("Reference", StringComparison.OrdinalIgnoreCase)
                    && i.Include.Equals(fileNameWithoutExt, StringComparison.OrdinalIgnoreCase)
                );

                if (existingRef == null)
                {
                    // 3. Add the Reference element
                    var newItem = itemGroup.AddItem("Reference", fileNameWithoutExt);
                    newItem.AddMetadata("HintPath", hintPath);

                    // 4. Add Private/CopyLocal metadata
                    newItem.AddMetadata("Private", copyToLocal.ToString().ToLower()); // "true" or "false"

                    string action = copyToLocal
                        ? "Added reference (and set Copy Local)"
                        : "Added reference (without Copy Local)";
                    logger.LogInformation("{Action} to '{FileNameWithoutExt}' from '{HintPath}'", action, fileNameWithoutExt, hintPath);
                    changesMade = true;
                }
                else
                {
                    logger.LogInformation("Reference to '{FileNameWithoutExt}' already exists, skipping.", fileNameWithoutExt);
                }
            }

            if (changesMade)
            {
                // 5. Save the modified .csproj file
                projectRoot.Save();
                logger.LogInformation("Modified .csproj file: {CsprojFilePath}", csprojFilePath);
                return true;
            }
            else
            {
                logger.LogInformation("No new references were added to .csproj file: {CsprojFilePath}", csprojFilePath);
                return true; // Still considered successful, no changes needed
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to modify .csproj file '{CsprojFilePath}'", csprojFilePath);
            return false;
        }
    }

    /// <summary>
    /// 向指定的 .csproj 文件添加一个或多个代码文件 (如 .cs, .vb, .fs)，并可以选择是否覆盖已存在的文件。
    /// 同时设置 EnableDefaultCompileItems 为 false 以避免冲突。
    /// </summary>
    /// <param name="csprojFilePath">.csproj 文件的完整路径</param>
    /// <param name="overwriteExisting">如果目标项目中已存在同名文件，是否覆盖 (默认 false)</param>
    /// <param name="sourceFilePaths">要添加的源代码文件路径数组</param>
    /// <returns>是否成功修改并保存文件</returns>
    public static bool AddSourceFiles(
        string csprojFilePath,
        bool overwriteExisting = false,
        params string[] sourceFilePaths
    )
    {
        if (!File.Exists(csprojFilePath))
        {
            logger.LogError(".csproj file does not exist: {CsprojFilePath}", csprojFilePath);
            return false;
        }

        if (sourceFilePaths == null || sourceFilePaths.Length == 0)
        {
            logger.LogInformation("No source file paths provided to add.");
            return true; // Nothing to do, but not an error
        }

        try
        {
            var projectRoot = ProjectRootElement.Open(csprojFilePath);

            // 1. 设置 EnableDefaultCompileItems 为 false
            if (!SetPropertyInternal(projectRoot, "EnableDefaultCompileItems", "false"))
            {
                logger.LogWarning("Could not set EnableDefaultCompileItems property, it might already be set or there was an issue.");
                // This might not be critical if it's already set, so we continue.
            }
            else
            {
                logger.LogInformation("Set EnableDefaultCompileItems to false.");
            }

            // Find or create an ItemGroup for Compile items (for .cs files)
            ProjectItemGroupElement compileItemGroup = null;
            foreach (var ig in projectRoot.ItemGroups)
            {
                if (
                    ig.Items.Any(i =>
                        i.ElementName.Equals("Compile", StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    compileItemGroup = ig;
                    break;
                }
            }

            if (compileItemGroup == null)
            {
                compileItemGroup = projectRoot.AddItemGroup();
            }

            bool changesMade = false;
            foreach (var sourcePath in sourceFilePaths)
            {
                if (!File.Exists(sourcePath))
                {
                    logger.LogWarning("Source file does not exist, skipping: {SourcePath}", sourcePath);
                    continue;
                }

                string fileName = Path.GetFileName(sourcePath);
                string relativePathWithinProject = fileName; // Or calculate relative path if needed
                string targetPathInProject = Path.Combine(
                    Path.GetDirectoryName(csprojFilePath),
                    relativePathWithinProject
                );

                if (File.Exists(targetPathInProject))
                {
                    if (overwriteExisting)
                    {
                        logger.LogInformation("Overwriting existing file: {TargetPathInProject}", targetPathInProject);
                        File.Copy(sourcePath, targetPathInProject, overwrite: true);
                    }
                    else
                    {
                        logger.LogInformation("File '{TargetPathInProject}' already exists in project directory. Skipping copy. (Set overwriteExisting=true to replace)", targetPathInProject);
                    }
                }
                else
                {
                    logger.LogInformation("Copying new file to project: {TargetPathInProject}", targetPathInProject);
                    File.Copy(sourcePath, targetPathInProject, overwrite: true);
                }

                string itemType = GetMsBuildItemTypeForExtension(Path.GetExtension(sourcePath));
                ProjectItemGroupElement targetItemGroup = GetOrCreateItemGroupForType(
                    projectRoot,
                    itemType
                );

                var existingCompileItem = targetItemGroup.Items.FirstOrDefault(i =>
                    i.ElementName.Equals(itemType, StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(i.Include)
                        .Equals(fileName, StringComparison.OrdinalIgnoreCase)
                );

                if (existingCompileItem == null)
                {
                    var newItem = targetItemGroup.AddItem(itemType, relativePathWithinProject);
                    logger.LogInformation("Added '{ItemType}' item '{RelativePathWithinProject}' to .csproj", itemType, relativePathWithinProject);
                    changesMade = true;
                }
                else
                {
                    logger.LogInformation("'{ItemType}' item '{RelativePathWithinProject}' already exists in .csproj, skipping.", itemType, relativePathWithinProject);
                }
            }

            if (changesMade)
            {
                projectRoot.Save();
                logger.LogInformation("Modified .csproj file: {CsprojFilePath}", csprojFilePath);
                return true;
            }
            else
            {
                logger.LogInformation("No new source items were added to .csproj file: {CsprojFilePath}", csprojFilePath);
                return true; // Still considered successful, no changes needed
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to modify .csproj file '{CsprojFilePath}' for source files", csprojFilePath);
            return false;
        }
    }

    /// <summary>
    /// Determines the MSBuild item type based on the file extension.
    /// </summary>
    /// <param name="extension">File extension including the dot (e.g., ".cs")</param>
    /// <returns>The corresponding MSBuild item type (e.g., "Compile", "EmbeddedResource")</returns>
    private static string GetMsBuildItemTypeForExtension(string extension)
    {
        switch (extension.ToLower())
        {
            case ".cs":
            case ".vb":
            case ".fs":
                return "Compile";
            case ".resx":
                return "EmbeddedResource";
            case ".config":
                return "None"; // Or "Content" depending on intent
            case ".txt":
            case ".json":
            case ".xml":
                // Could be None, Content, or EmbeddedResource depending on usage
                // Defaulting to None for general text files
                return "None";
            // Add more cases as needed
            default:
                // For unknown extensions, default to 'None' or 'Content'
                // You might want to make this configurable or prompt the user
                return "None";
        }
    }

    /// <summary>
    /// Finds an existing ItemGroup for a specific item type, or creates a new one.
    /// </summary>
    /// <param name="projectRoot">The root element of the project file.</param>
    /// <param name="itemType">The item type (e.g., "Compile", "EmbeddedResource").</param>
    /// <returns>The found or created ItemGroup.</returns>
    private static ProjectItemGroupElement GetOrCreateItemGroupForType(
        ProjectRootElement projectRoot,
        string itemType
    )
    {
        foreach (var ig in projectRoot.ItemGroups)
        {
            if (
                ig.Items.Any(i =>
                    i.ElementName.Equals(itemType, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                return ig;
            }
        }
        // Create a new ItemGroup if not found
        return projectRoot.AddItemGroup();
    }

    /// <summary>
    /// Internal helper to set a property on a ProjectRootElement.
    /// </summary>
    private static bool SetPropertyInternal(
        ProjectRootElement projectRoot,
        string propertyName,
        string propertyValue
    )
    {
        // Find the first PropertyGroup or create one if none exists
        var propertyGroup = projectRoot.PropertyGroups.FirstOrDefault();

        if (propertyGroup == null)
        {
            propertyGroup = projectRoot.AddPropertyGroup();
        }

        // Find the property or create one if it doesn't exist
        var existingProperty = propertyGroup.Properties.FirstOrDefault(p =>
            p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)
        );

        if (existingProperty != null)
        {
            // Update existing property
            if (existingProperty.Value != propertyValue)
            {
                existingProperty.Value = propertyValue;
                return true; // Changed
            }
            // Already has the desired value
            return false; // Not changed
        }
        else
        {
            // Add new property
            propertyGroup.AddProperty(propertyName, propertyValue);
            return true; // Added
        }
    }

    /// <summary>
    /// 将 CSharpLanguageVersion 枚举转换为对应的字符串
    /// </summary>
    /// <param name="version">语言版本枚举</param>
    /// <returns>对应的字符串表示</returns>
    private static string GetLanguageVersionString(CSharpLanguageVersion version)
    {
        return version switch
        {
            CSharpLanguageVersion.Latest => "latest",
            CSharpLanguageVersion.Preview => "preview",
            CSharpLanguageVersion.CSharp3 => "3.0",
            CSharpLanguageVersion.CSharp4 => "4.0",
            CSharpLanguageVersion.CSharp5 => "5.0",
            CSharpLanguageVersion.CSharp6 => "6.0",
            CSharpLanguageVersion.CSharp7 => "7.0",
            CSharpLanguageVersion.CSharp8 => "8.0",
            CSharpLanguageVersion.CSharp9 => "9.0",
            CSharpLanguageVersion.CSharp10 => "10.0",
            CSharpLanguageVersion.CSharp11 => "11.0",
            CSharpLanguageVersion.CSharp12 => "12.0",
            CSharpLanguageVersion.CSharp13 => "13.0",
            _ => "latest",
        };
    }

    /// <summary>
    /// 向指定的 .csproj 文件添加 NuGet 包引用
    /// </summary>
    /// <param name="csprojFilePath">.csproj 文件的完整路径</param>
    /// <param name="packageName">NuGet 包名称</param>
    /// <param name="version">NuGet 包版本（可选）</param>
    /// <returns>是否成功修改并保存文件</returns>
    public static bool AddNuGetPackage(
        string csprojFilePath,
        string packageName,
        string version 
    )
    {
        if (!File.Exists(csprojFilePath))
        {
            logger.LogError(".csproj file does not exist: {CsprojFilePath}", csprojFilePath);
            return false;
        }

        if (string.IsNullOrWhiteSpace(packageName))
        {
            logger.LogError("Package name cannot be null or empty.");
            return false;
        }

        try
        {
            // 加载 .csproj 文件
            var projectRoot = ProjectRootElement.Open(csprojFilePath);

            // 查找或创建 ItemGroup 来放置 PackageReference
            ProjectItemGroupElement itemGroup = null;
            foreach (var ig in projectRoot.ItemGroups)
            {
                if (
                    ig.Items.Any(i =>
                        i.ElementName.Equals("PackageReference", StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    itemGroup = ig;
                    break;
                }
            }

            if (itemGroup == null)
            {
                itemGroup = projectRoot.AddItemGroup();
            }

            // 检查是否已经存在相同的包引用
            var existingPackage = itemGroup.Items.FirstOrDefault(i =>
                i.ElementName.Equals("PackageReference", StringComparison.OrdinalIgnoreCase)
                && i.Include.Equals(packageName, StringComparison.OrdinalIgnoreCase)
            );

            if (existingPackage != null)
            {
                logger.LogInformation("Package '{PackageName}' already exists in the project, skipping.", packageName);
                return true;
            }

            // 添加新的 PackageReference
            var packageItem = itemGroup.AddItem("PackageReference", packageName);

            // 如果指定了版本，则添加 Version 元数据
            if (!string.IsNullOrWhiteSpace(version))
            {
                packageItem.AddMetadata("Version", version);
            }

            // 保存修改后的 .csproj 文件
            projectRoot.Save();
            logger.LogInformation("Added NuGet package '{PackageName}' to .csproj file: {CsprojFilePath}", packageName, csprojFilePath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add NuGet package '{PackageName}' to .csproj file '{CsprojFilePath}'", packageName, csprojFilePath);
            return false;
        }
    }
    
    /// <summary>
    /// 设置 NuGet 包的元数据（如 ExcludeAssets、PrivateAssets 等）
    /// </summary>
    /// <param name="csprojFilePath">.csproj 文件的完整路径</param>
    /// <param name="packageName">NuGet 包名称</param>
    /// <param name="excludeAssets">ExcludeAssets 参数（可选）</param>
    /// <param name="privateAssets">PrivateAssets 参数（可选）</param>
    /// <returns>是否成功设置元数据</returns>
    public static bool SetNuGetPackageMetadata(
        string csprojFilePath,
        string packageName,
        string excludeAssets = null,
        string privateAssets = null)
    {
        if (!File.Exists(csprojFilePath))
        {
            logger.LogError(".csproj file does not exist: {CsprojFilePath}", csprojFilePath);
            return false;
        }

        if (string.IsNullOrWhiteSpace(packageName))
        {
            logger.LogError("Package name cannot be null or empty.");
            return false;
        }

        try
        {
            // 加载 .csproj 文件
            var projectRoot = ProjectRootElement.Open(csprojFilePath);

            // 查找包含 PackageReference 的 ItemGroup
            var itemGroup = projectRoot.ItemGroups
                .FirstOrDefault(ig => ig.Items.Any(i =>
                    i.ElementName.Equals("PackageReference", StringComparison.OrdinalIgnoreCase) &&
                    i.Include.Equals(packageName, StringComparison.OrdinalIgnoreCase)));

            if (itemGroup == null)
            {
                logger.LogError("Package '{PackageName}' not found in project: {CsprojFilePath}", packageName, csprojFilePath);
                return false;
            }

            // 查找指定的包引用
            var packageItem = itemGroup.Items.FirstOrDefault(i =>
                i.ElementName.Equals("PackageReference", StringComparison.OrdinalIgnoreCase) &&
                i.Include.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (packageItem == null)
            {
                logger.LogError("Package '{PackageName}' not found in project: {CsprojFilePath}", packageName, csprojFilePath);
                return false;
            }

            // 更新或添加 ExcludeAssets 元数据
            if (!string.IsNullOrWhiteSpace(excludeAssets))
            {
                UpdateOrAddMetadata(packageItem, "ExcludeAssets", excludeAssets);
            }

            // 更新或添加 PrivateAssets 元数据
            if (!string.IsNullOrWhiteSpace(privateAssets))
            {
                UpdateOrAddMetadata(packageItem, "PrivateAssets", privateAssets);
            }

            // 保存修改后的 .csproj 文件
            projectRoot.Save();
            logger.LogInformation("Updated metadata for NuGet package '{PackageName}' in project: {CsprojFilePath}", packageName, csprojFilePath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while setting metadata for NuGet package '{PackageName}'", packageName);
            return false;
        }
    }
    
    /// <summary>
    /// 更新或添加元数据项
    /// </summary>
    /// <param name="item">要更新的项目元素</param>
    /// <param name="metadataName">元数据名称</param>
    /// <param name="metadataValue">元数据值</param>
    private static void UpdateOrAddMetadata(ProjectItemElement item, string metadataName, string metadataValue)
    {
        var existingMetadata = item.Metadata.FirstOrDefault(m => m.Name.Equals(metadataName, StringComparison.OrdinalIgnoreCase));
        
        if (existingMetadata != null)
        {
            // 如果元数据已存在，更新它的值
            if (!existingMetadata.Value.Equals(metadataValue, StringComparison.OrdinalIgnoreCase))
            {
                existingMetadata.Value = metadataValue;
                logger.LogInformation("Updated metadata '{MetadataName}' from '{OldValue}' to '{NewValue}'", metadataName, existingMetadata.Value, metadataValue);
            }
            else
            {
                logger.LogInformation("Metadata '{MetadataName}' already set to '{MetadataValue}', no change needed", metadataName, metadataValue);
            }
        }
        else
        {
            // 如果元数据不存在，添加新的元数据
            item.AddMetadata(metadataName, metadataValue);
            logger.LogInformation("Added metadata '{MetadataName}' with value '{MetadataValue}'", metadataName, metadataValue);
        }
    }
}