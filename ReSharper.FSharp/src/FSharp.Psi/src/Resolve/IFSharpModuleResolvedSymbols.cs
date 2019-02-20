using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public interface IFSharpModuleResolvedSymbols
  {
    void Invalidate();

    void Invalidate(IPsiSourceFile sourceFile);

    [NotNull]
    FSharpFileResolvedSymbols GetResolvedSymbols(IPsiSourceFile sourceFile);
  }
}
