using System;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Util.Storage;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.BuildScript
{
    public class CopyFantomasRuntimeConfig
    {
        [BuildStep]
        public static SubplatformFileForPackagingFast[] Run(AllAssembliesOnEverything allass, ProductHomeDirArtifact homedir)
        {
            if (allass.FindSubplatformByClass<CopyFantomasRuntimeConfig>() is SubplatformOnSources subplatform)
            {
                return new[]
                {
                    CopyFileToOutputRequest("Fantomas.Host.unix.runtimeconfig.json"),
                    CopyFileToOutputRequest("Fantomas.Host.win.runtimeconfig.json")
                };

                SubplatformFileForPackagingFast CopyFileToOutputRequest(string fileName)
                {
                    return new SubplatformFileForPackagingFast(
                        subplatform.Name,
                        ImmutableFileItem.CreateFromDisk(homedir.ProductHomeDir / subplatform.Name.RelativePath / fileName));
                }
            }

            return Array.Empty<SubplatformFileForPackagingFast>();
        }
    }
}