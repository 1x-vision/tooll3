using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX.D3DCompiler;
using T3.Core.Logging;
using T3.Core.SystemUi;

namespace T3.Core.Resource;

public abstract partial class ShaderCompiler
{
    public static void DeleteShaderCache(bool all)
    {
        int failures = 0;
        int total = 0;
        long totalFileSize = 0;

        var directory = all ? ShaderCacheRootPath : _shaderCacheDirectory;
        if(!Directory.Exists(directory))
        {
            BlockingWindow.Instance.ShowMessageBox($"No shader cache found at \"{directory}\".", "No shader cache found");
            return;
        }
        
        lock (ShaderCacheLock)
        {
            Directory.EnumerateFiles(directory, $"*{FileExtension}", SearchOption.AllDirectories)
                     .AsParallel()
                     .ForAll(file =>
                             {
                                 Interlocked.Increment(ref total);
                                 try
                                 {
                                     var fileInfo = new FileInfo(file);
                                     var fileSize = fileInfo.Length;
                                     fileInfo.Delete();
                                     Interlocked.Add(ref totalFileSize, fileSize);
                                 }
                                 catch (Exception e)
                                 {
                                     Log.Error($"Failed to delete shader cache file '{file}': {e.Message}");
                                     Interlocked.Increment(ref failures);
                                 }
                             });
        }

        var deletionsInKb = totalFileSize / 1024d;
        var deletionsString = deletionsInKb < 1024d
                                  ? $"{deletionsInKb:0.0} KB"
                                  : $"{deletionsInKb / 1024d:0.0} MB";
        
        var isError = failures > 0;

        string message;
        string title;
        if (isError)
        {
            message = $"Failed to delete {failures} out of {total} shader cache files.";
            title = "Shader Cache Deletion Failed";
        }
        else
        {
            message = string.Empty;
            title = "Shader Cache Deleted Successfully";
        }

        var finalMessage = $"Deleted {deletionsString} of shader cache from \"{ShaderCacheRootPath}\".\n{message}\n" +
                           $"Restart the application to refresh the removed cache, as all shaders still reside in memory.";
        BlockingWindow.Instance.ShowMessageBox(finalMessage, title);
    }

    private static void CacheSuccessfulCompilation(ShaderBytecode oldBytecode, ulong hash, ShaderBytecode newBytecode)
    {
        CacheShaderInMemory(oldBytecode, hash, newBytecode);
        SaveBytecodeToDisk(hash, newBytecode);
    }

    private static void SaveBytecodeToDisk(ulong hash, ShaderBytecode bytecode)
    {
        if (!_diskCachingEnabled)
        {
            return;
        }
        
        var path = GetPathForShaderCache(hash);
        File.WriteAllBytes(path, bytecode.Data);
    }

    private static bool TryLoadBytecodeFromDisk(ulong hash, [NotNullWhen(true)] out ShaderBytecode bytecode)
    {
        if (!_diskCachingEnabled)
        {
            bytecode = null;
            return false;
        }
        
        var path = GetPathForShaderCache(hash);
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            bytecode = null;
            return false;
        }

        var data = File.ReadAllBytes(path);
        bytecode = new ShaderBytecode(data);
        return true;
    }

    private static void CacheShaderInMemory(ShaderBytecode oldBytecode, ulong hash, ShaderBytecode newBytecode)
    {
        if (oldBytecode != null)
        {
            if (ShaderBytecodeHashes.Remove(oldBytecode, out var oldHash))
            {
                ShaderBytecodeCache.Remove(oldHash);
            }
        }

        ShaderBytecodeCache[hash] = newBytecode;
        ShaderBytecodeHashes[newBytecode] = hash;
    }

    private static string GetPathForShaderCache(ulong hashCode)
    {
        // ReSharper disable once StringLiteralTypo
        return Path.Combine(_shaderCacheDirectory, hashCode + FileExtension);
    }

    private static readonly Dictionary<ShaderBytecode, ulong> ShaderBytecodeHashes = new();
    private static readonly Dictionary<ulong, ShaderBytecode> ShaderBytecodeCache = new();
    
    [StructLayout(LayoutKind.Explicit)]
    private readonly struct ULongFromTwoInts(int a, int b)
    {
        [FieldOffset(0)]
        public readonly ulong Value = 0;
        
        [FieldOffset(0)]
        public readonly int A = a;
        
        [FieldOffset(4)]
        public readonly int B = b;
    }
    
    private static readonly object ShaderCacheLock = new();
    private static readonly string ShaderCacheRootPath = Path.Combine(UserData.UserData.TempFolder, "cache");
    private static string _shaderCacheDirectory;

    public static string ShaderCacheSubdirectory
    {
        set
        {
            if(_shaderCacheDirectory != null)
                throw new InvalidOperationException("Shader cache subdirectory can only be set once.");
            
            _shaderCacheDirectory = Path.Combine(ShaderCacheRootPath, value);
            
            var potentialCacheFilePath = Path.Combine(_shaderCacheDirectory, ulong.MaxValue + FileExtension);

            if (potentialCacheFilePath.Length > 259)
            {
                string message = $"File path for shader cache is too long: \"{_shaderCacheDirectory}\". Disk caching will be disabled.";
                BlockingWindow.Instance.ShowMessageBox(message);
                Log.Error(message);
                _diskCachingEnabled = false;
                return;
            }

            Directory.CreateDirectory(_shaderCacheDirectory);
        }
    }

    private static bool _diskCachingEnabled = true;
    private const string FileExtension = ".shadercache";
}