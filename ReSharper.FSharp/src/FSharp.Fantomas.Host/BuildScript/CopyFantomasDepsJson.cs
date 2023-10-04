using System;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Util.Storage;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.BuildScript
{
    public class CopyFantomasDepsJson
    {
        [BuildStep]
        public static SubplatformFileForPackagingFast[] Run(AllAssembliesOnEverything allass, ProductHomeDirArtifact homedir)
        {
            return allass.FindSubplatformByClass<CopyFantomasDepsJson>() is SubplatformOnSources subplatform ? new[] {new SubplatformFileForPackagingFast(subplatform.Name, ImmutableFileItem.CreateFromDisk(homedir.ProductHomeDir / subplatform.Name.RelativePath / "JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.deps.json"))} : Array.Empty<SubplatformFileForPackagingFast>();
        }
    }
}