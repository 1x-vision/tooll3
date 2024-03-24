using System.IO;
using System.Linq;
using System.Threading;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Animation;
using T3.Core.Compilation;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Player;

internal static partial class Program
{
    private static void LoadOperators()
    {
        var searchDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Operators");
        Log.Info($"Loading operators from \"{searchDirectory}\"...");

        var assemblies = Directory.GetDirectories(searchDirectory, "*", SearchOption.TopDirectoryOnly)
                                  .SelectMany(packageDir =>
                                              {
                                                  Log.Debug($"Searching for dlls in {packageDir}...");
                                                  return Directory.GetFiles(packageDir, "*.dll", SearchOption.TopDirectoryOnly)
                                                                  .Select(file =>
                                                                          {
                                                                              var relativePath = Path.GetRelativePath(searchDirectory, file);
                                                                              Log.Debug($"Found dll: {relativePath}");
                                                                              RuntimeAssemblies.TryLoadAssemblyInformation(file, out var info);
                                                                              return info;
                                                                          })
                                                                  .Where(info =>
                                                                         {
                                                                             var isOperatorAssembly = info.IsOperatorAssembly;
                                                                             Log.Debug($"{info.Name} is operator assembly: {isOperatorAssembly}");
                                                                             return isOperatorAssembly;
                                                                         });
                                              }).ToArray();
        
        Log.Debug($"Finished loading {assemblies.Length} operator assemblies. Loading symbols...");
        var packageLoadInfo = assemblies
                             .AsParallel()
                             .Select(assemblyInfo =>
                                     {
                                         var symbolPackage = new PlayerSymbolPackage(assemblyInfo);
                                         symbolPackage.InitializeResources();
                                         symbolPackage.LoadSymbols(false, out var newSymbolsWithFiles, out _, false);
                                         return new PackageLoadInfo(symbolPackage, newSymbolsWithFiles);
                                     })
                             .ToArray();

        packageLoadInfo
           .AsParallel()
           .ForAll(packageInfo => packageInfo.Package.ApplySymbolChildren(packageInfo.NewlyLoadedSymbols));
    }
    
    private static void PreloadShadersAndResources(double durationSecs,
                                                   Int2 resolution,
                                                   Playback playback,
                                                   DeviceContext deviceContext,
                                                   EvaluationContext context,
                                                   Slot<Texture2D> textureOutput,
                                                   SwapChain swapChain,
                                                   RenderTargetView renderView)
    {
        var previousSpeed = playback.PlaybackSpeed;
        var originalTime = playback.TimeInSecs;

        playback.PlaybackSpeed = 0.1f;
        var rasterizer = deviceContext.Rasterizer;
        var merger = deviceContext.OutputMerger;
        var hasTextureOutput = textureOutput != null;

        for (double timeInSecs = 0; timeInSecs < durationSecs; timeInSecs += 2.0)
        {
            playback.TimeInSecs = timeInSecs;
            Log.Info($"Pre-evaluate at: {timeInSecs:0.00}s / {playback.TimeInBars:0.00} bars");

            DirtyFlag.IncrementGlobalTicks();
            DirtyFlag.InvalidationRefFrame++;

            rasterizer.SetViewport(new Viewport(0, 0, resolution.Width, resolution.Height, 0.0f, 1.0f));
            merger.SetTargets(renderView);

            context.Reset();
            context.RequestedResolution = resolution;

            if (hasTextureOutput)
            {
                textureOutput.Invalidate();
                textureOutput.GetValue(context); // why is this done twice?

                if (textureOutput.GetValue(context) == null)
                {
                    Log.Error("Failed to initialize texture");
                }
            }

            Thread.Sleep(20);
            swapChain.Present(1, PresentFlags.None);
        }

        playback.PlaybackSpeed = previousSpeed;
        playback.TimeInSecs = originalTime;
    }
}