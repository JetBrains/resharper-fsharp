using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
{
  public class FSharpCompiledStruct : Struct, IFSharpCompiledTypeElement
  {
    [NotNull] private FSharpDeclaredName FSharpName { get; }
    public FSharpCompiledTypeRepresentation Representation { get; }
    public FSharpAccessRights FSharpAccessRights { get; }

    public ICacheTrieNode AlternativeNameTrieNode { get; set; }

    public FSharpCompiledStruct([CanBeNull] FSharpMetadataEntity entity, [NotNull] ICompiledEntity parent,
      [NotNull] IReflectionBuilder builder, [NotNull] IMetadataTypeInfo info) : base(parent, builder, info)
    {
      FSharpName = FSharpMetadataEntityModule.getCompiledModuleDeclaredName(entity);
      Representation = FSharpMetadataEntityModule.getRepresentation(entity);
      FSharpAccessRights = entity.GetFSharpAccessRights();
    }

    public string SourceName => FSharpName.SourceName;
    public string AlternativeName => FSharpName.AlternativeName;
    public ModuleMembersAccessKind AccessKind => ModuleMembersAccessKind.Normal; // todo
  }
}
