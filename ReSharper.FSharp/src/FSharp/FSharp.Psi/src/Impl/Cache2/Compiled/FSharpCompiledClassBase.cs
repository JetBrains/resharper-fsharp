using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
{
  public abstract class FSharpCompiledClassBase(
    [CanBeNull] FSharpMetadataEntity entity,
    [NotNull] ICompiledEntity parent,
    [NotNull] IReflectionBuilder builder,
    [NotNull] IMetadataTypeInfo info)
    : Class(parent, builder, info), IFSharpCompiledTypeElement
  {
    [NotNull] private FSharpDeclaredName FSharpName { get; } = FSharpMetadataEntityModule.getCompiledModuleDeclaredName(entity);
    public FSharpCompiledTypeRepresentation Representation { get; } = FSharpMetadataEntityModule.getRepresentation(entity);
    public FSharpAccessRights FSharpAccessRights { get; } = entity.GetFSharpAccessRights();

    public ICacheTrieNode AlternativeNameTrieNode { get; set; }

    public string SourceName => FSharpName.SourceName;
    public virtual DeclaredElementType FSharpElementType => this.TryGetFSharpDeclaredElementType();
    string IAlternativeNameOwner.AlternativeName => FSharpName.AlternativeName;
    public virtual ModuleMembersAccessKind AccessKind => ModuleMembersAccessKind.Normal; // todo
  }
}
