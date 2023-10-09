using JetBrains.Build;
using JetBrains.ReSharper.Plugins.FSharp.RiderPlugin.BuildScript;
using JetBrains.Rider.Backend.Install;

namespace JetBrains.ReSharper.Plugins.FSharp.RiderPlugin.Install;

public static class AdvertiseRiderBundledPlugin
{
  [BuildStep]
  public static RiderBundledProductArtifact[] ShipFSharpWithRider()
  {
    return new[]
    {
      new RiderBundledProductArtifact(
        FSharpInRiderProduct.ProductTechnicalName,
        FSharpInRiderProduct.ThisSubplatformName,
        FSharpInRiderProduct.DotFilesFolder,
        allowCommonPluginFiles: false),
      new RiderBundledProductArtifact(
        FSharpInRiderProduct.Fantomas.ProductTechnicalName,
        FSharpInRiderProduct.Fantomas.FantomasSubplatformName,
        FSharpInRiderProduct.Fantomas.FantomasFolder,
        allowCommonPluginFiles: false),
      new RiderBundledProductArtifact(
        FSharpInRiderProduct.TypeProviders.ProductTechnicalName,
        FSharpInRiderProduct.TypeProviders.TypeProvidersSubplatformName,
        FSharpInRiderProduct.TypeProviders.TypeProvidersFolder,
        allowCommonPluginFiles: false)
    };
  }
}