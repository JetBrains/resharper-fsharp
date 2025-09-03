using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpDeclaredElement : IClrDeclaredElement
  {
    string SourceName { get; }
  }

  public interface IFSharpTypeElement : IFSharpDeclaredElement, ITypeElement, IAccessRightsOwner
  {
    ModuleMembersAccessKind AccessKind { get; }
  }

  public interface IFSharpSourceTypeElement : IFSharpTypeElement
  {
    [CanBeNull] ITypeDeclaration DefiningDeclaration { get; }

    [CanBeNull] internal TypePart Parts { get; }
    [NotNull] internal IEnumerable<TypePart> EnumerateParts();
  }

  public interface IFSharpCompiledTypeElement : IFSharpTypeElement, ICompiledTypeElement,
    IAlternativeNameCacheTrieNodeOwner
  {
    FSharpCompiledTypeRepresentation Representation { get; }
    FSharpAccessRights FSharpAccessRights { get; }
  }
}
