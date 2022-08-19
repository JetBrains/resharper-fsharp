using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IReferenceNameOwnerPat
  {
    [CanBeNull] FSharpSymbolReference Reference { get; }
  }
}
