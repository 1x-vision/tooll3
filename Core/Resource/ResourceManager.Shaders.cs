#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Core.Resource;

/// <summary>
/// Creates or loads shaders as "resources" and handles their filehooks, compilation, etc
/// Could do with some simplification - perhaps their arguments should be condensed into a struct?
/// </summary>
public sealed partial class ResourceManager
{
    public bool TryCreateShaderResourceFromSource<TShader>(out ShaderResource<TShader> resource, string shaderSource, Instance instance,
                                                           out string errorMessage,
                                                           string name = "", string entryPoint = "main")
        where TShader : class, IDisposable
    {
        var resourceId = GetNextResourceId();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"{typeof(TShader).Name}_{resourceId}";
        }

        var compiled = ShaderCompiler.Instance.TryCreateShaderResourceFromSource<TShader>(shaderSource: shaderSource,
                                                                                          name: name,
                                                                                          directory: instance.AvailableResourceFolders,
                                                                                          entryPoint: entryPoint,
                                                                                          resourceId: resourceId,
                                                                                          resource: out var newResource,
                                                                                          errorMessage: out errorMessage);

        if (compiled)
        {
            ResourcesById.TryAdd(newResource.Id, newResource);
        }
        else
        {
            Log.Error($"Failed to compile shader '{name}'");
        }

        resource = newResource;
        return compiled;
    }

    public bool TryCreateShaderResource<TShader>(out ShaderResource<TShader>? resource, Instance? instance, string relativePath,
                                                 out string errorMessage,
                                                 string name = "", string entryPoint = "main", Action? fileChangedAction = null)
        where TShader : class, IDisposable
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            resource = null;
            errorMessage = "Empty file name";
            return false;
        }

        if (!TryResolvePath(relativePath, instance, out var path, out var resourceContainer))
        {
            resource = null;
            errorMessage = $"Path not found: '{relativePath}' (Resolved to '{path}').";
            return false;
        }

        var fileInfo = new FileInfo(path);
        if (string.IsNullOrWhiteSpace(name))
            name = fileInfo.Name;

        ResourceFileHook? fileHook = null;
        var fileWatcher = resourceContainer?.FileWatcher;
        var hasFileWatcher = fileWatcher != null;
        var resourceFolder = resourceContainer?.ResourcesFolder;
        var hookExists = hasFileWatcher && fileWatcher!.HooksForResourceFilePaths.TryGetValue(relativePath, out fileHook);
        if (hookExists)
        {
            foreach (var id in fileHook!.ResourceIds)
            {
                var resourceById = ResourcesById[id];
                if (resourceById is not ShaderResource<TShader> shaderResource || shaderResource.EntryPoint != entryPoint)
                    continue;

                if (fileChangedAction != null)
                {
                    fileHook.FileChangeAction -= fileChangedAction;
                    fileHook.FileChangeAction += fileChangedAction;
                }

                resource = shaderResource;
                errorMessage = string.Empty;
                return true;
            }
        }

        // need to create
        var resourceId = GetNextResourceId();
        List<string> compilationReferences = new();

        if (instance != null)
            compilationReferences.AddRange(instance.AvailableResourceFolders);
        else if (resourceFolder != null)
            compilationReferences.Add(resourceFolder);

        var compiled = ShaderCompiler.Instance.TryCreateShaderResourceFromFile(srcFile: path,
                                                                               entryPoint: entryPoint,
                                                                               name: name,
                                                                               resourceId: resourceId,
                                                                               resource: out resource,
                                                                               errorMessage: out errorMessage,
                                                                               resourceDirs: compilationReferences);

        if (!compiled)
        {
            Log.Error($"Failed to compile shader '{path}'");
            return false;
        }

        ResourcesById.TryAdd(resource!.Id, resource);
        if (resourceContainer == null)
            return true;

        if (hasFileWatcher)
        {
            if (fileHook == null)
            {
                fileHook = new ResourceFileHook(path, new[] { resourceId });
                fileWatcher!.HooksForResourceFilePaths.TryAdd(relativePath, fileHook);
            }

            if (fileChangedAction != null)
            {
                fileHook.FileChangeAction -= fileChangedAction;
                fileHook.FileChangeAction += fileChangedAction;
            }
        }
        #if DEBUG
        else if (fileChangedAction != null)
        {
            const string logFmt = "File watcher not enabled for resource '{0}'. It likely comes from a read-only resource container.";
            Log.Debug(string.Format(logFmt, relativePath));
        }
        #endif

        return true;
    }
}