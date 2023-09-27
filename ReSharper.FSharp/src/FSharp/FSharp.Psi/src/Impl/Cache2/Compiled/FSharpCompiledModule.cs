using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi.Impl.Reflection2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
{
  public class FSharpCompiledModule : FSharpCompiledClassBase
  {
    public FSharpCompiledModule(FSharpMetadataEntity entity, [NotNull] ICompiledEntity parent,
      [NotNull] IReflectionBuilder builder, [NotNull] IMetadataTypeInfo info) : base(entity, parent, builder, info)
    {
    }
  }
}
