using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.BuildScript
{
    public class ExemptFantomasFromProductAssemblies
    {
        [BuildStep]
        public static SubplatformWithNonProductAssemblies[] MarkMyselfNonProduct(AllAssembliesOnEverything allass)
        {
            return SubplatformWithNonProductAssemblies.MarkCaller<ExemptFantomasFromProductAssemblies>(allass);
        }
    }
}