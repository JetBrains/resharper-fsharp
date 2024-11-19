using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.Impl.Reflection2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpDeclaredElement : IClrDeclaredElement
  {
    string SourceName { get; }
  }

  public interface IFSharpTypeElement : IFSharpDeclaredElement, ITypeElement
  {
    ModuleMembersAccessKind AccessKind { get; }
  }

  public interface IFSharpCompiledTypeElement : ICompiledTypeElement, IFSharpDeclaredElement,
    IAlternativeNameCacheTrieNodeOwner
  {
    FSharpCompiledTypeRepresentation Representation { get; }
  }
}
