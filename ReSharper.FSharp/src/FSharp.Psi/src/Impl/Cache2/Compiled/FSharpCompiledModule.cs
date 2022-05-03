using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
{
  public class FSharpCompiledModule : Class, IFSharpCompiledTypeElement
  {
    [NotNull] private FSharpDeclaredName FSharpName { get; }

    public CacheTrieNode AlternativeNameTrieNode { get; set; }

    public FSharpCompiledModule(FSharpDeclaredName fsName, [NotNull] ICompiledEntity parent,
      [NotNull] IReflectionBuilder builder,
      [NotNull] IMetadataTypeInfo info) : base(parent, builder, info)
    {
      FSharpName = fsName;
    }

    public string SourceName => FSharpName.SourceName;
    public string AlternativeName => FSharpName.AlternativeName;
  }
}
