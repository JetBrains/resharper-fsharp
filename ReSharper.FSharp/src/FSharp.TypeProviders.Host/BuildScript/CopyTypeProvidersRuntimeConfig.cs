using System;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Util.Storage;

namespace JetBrains.ReSharper.Plugins.FSharp.BuildScript;

public class CopyTypeProvidersRuntimeConfig
{
    [BuildStep]
    public static SubplatformFileForPackagingFast[] Run(AllAssembliesOnEverything allass, ProductHomeDirArtifact homedir)
    {
        if (allass.FindSubplatformByClass<CopyTypeProvidersRuntimeConfig>() is SubplatformOnSources subplatform)
        {
            return new[]
            {
                CopyFileToOutputRequest("tploader.unix.runtimeconfig.json"),
                CopyFileToOutputRequest("tploader.win.runtimeconfig.json")
            };

            SubplatformFileForPackagingFast CopyFileToOutputRequest(string fileName)
            {
                return new SubplatformFileForPackagingFast(
                    subplatform.Name,
                    ImmutableFileItem.CreateFromDisk(homedir.ProductHomeDir / subplatform.Name.RelativePath / "FSharp.TypeProviders.Host.NetCore" / fileName));
            }
        }

        return Array.Empty<SubplatformFileForPackagingFast>();
    }
}