using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public interface IFSharpTypePart
  {
    [NotNull] string SourceName { get; }
    int MeasureTypeParametersCount { get; }
    TypePart GetFirstPart();

    ModuleMembersAccessKind AccessKind { get; }
  }
}
