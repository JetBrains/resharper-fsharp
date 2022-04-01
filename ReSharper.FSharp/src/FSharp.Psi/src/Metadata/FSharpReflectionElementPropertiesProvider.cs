using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpReflectionElementPropertiesProvider : ReflectionElementPropertiesProvider
  {
    public FSharpMetadata Metadata { get; }

    public FSharpReflectionElementPropertiesProvider(FSharpMetadata metadata)
    {
      Metadata = metadata;
      ClassProperties = new FSharpCompiledClassFactory(metadata);
    }

    public override CompiledTypeElementFactory ClassProperties { get; }

    public class FSharpCompiledClassFactory : ClassFactory
    {
      public FSharpMetadata Metadata { get; }

      public FSharpCompiledClassFactory(FSharpMetadata metadata) =>
        Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder,
        IMetadataTypeInfo info) =>
        Metadata.Modules.TryGetValue(info.FullyQualifiedName, out var module)
          ? new FSharpCompiledModule(module.FSharpName, parent, builder, info)
          : base.Create(parent, builder, info);
    }
  }
}
