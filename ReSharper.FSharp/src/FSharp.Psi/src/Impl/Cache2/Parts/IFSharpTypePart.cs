using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Naming;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public interface IFSharpTypePart
  {
    [NotNull]
    FSharpName FSharpName { get; }
  }
}
