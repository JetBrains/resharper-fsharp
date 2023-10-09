using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;

namespace JetBrains.ReSharper.Plugins.FSharp.BuildScript;

public class ExemptTypeProvidersFromProductAssemblies
{
    [BuildStep]
    public static SubplatformWithNonProductAssemblies[] MarkMyselfNonProduct(AllAssembliesOnEverything allass)
    {
        return SubplatformWithNonProductAssemblies.MarkCaller<ExemptTypeProvidersFromProductAssemblies>(allass);
    }
}