using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpReflectionElementPropertiesProvider(FSharpMetadata metadata) : ReflectionElementPropertiesProvider
  {
    public FSharpMetadata Metadata { get; } = metadata;

    public override CompiledTypeElementFactory ClassProperties { get; } = new FSharpCompiledClassFactory(metadata);
    public override CompiledTypeElementFactory DelegateProperties { get; } = new FSharpCompiledDelegateFactory(metadata);
    // public override CompiledTypeElementFactory EnumProperties { get; } = new FSharpCompiledEnumFactory(metadata);
    public override CompiledTypeElementFactory InterfaceProperties { get; } = new FSharpCompiledInterfaceFactory(metadata);
    public override CompiledTypeElementFactory StructProperties { get; } = new FSharpCompiledStructFactory(metadata);

    public class FSharpCompiledClassFactory(FSharpMetadata metadata) : ClassFactory
    {
      public FSharpMetadata Metadata { get; } = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info)
      {
        if (!Metadata.EntitiesByName.TryGetValue(info.FullyQualifiedName, out var entity))
          return base.Create(parent, builder, info);

        return entity.Representation is FSharpCompiledTypeRepresentation.Module moduleRepresentation
          ? new FSharpCompiledModule(moduleRepresentation, entity, parent, builder, info)
          : new FSharpCompiledClass(entity, parent, builder, info);
      }
    }

    public class FSharpCompiledStructFactory(FSharpMetadata metadata) : StructFactory
    {
      public FSharpMetadata Metadata { get; } = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info) =>
        !Metadata.EntitiesByName.TryGetValue(info.FullyQualifiedName, out var entity)
          ? base.Create(parent, builder, info)
          : new FSharpCompiledStruct(entity, parent, builder, info);
    }

    public class FSharpCompiledInterfaceFactory(FSharpMetadata metadata) : InterfaceFactory
    {
      public FSharpMetadata Metadata { get; } = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info) =>
        !Metadata.EntitiesByName.TryGetValue(info.FullyQualifiedName, out var entity)
          ? base.Create(parent, builder, info)
          : new FSharpCompiledInterface(entity, parent, builder, info);
    }

    // public class FSharpCompiledEnumFactory(FSharpMetadata metadata) : InterfaceFactory
    // {
    //   public FSharpMetadata Metadata { get; } = metadata;
    //
    //   public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info) =>
    //     !Metadata.EntitiesByName.TryGetValue(info.FullyQualifiedName, out var entity)
    //       ? base.Create(parent, builder, info)
    //       : new FSharpCompiledInterface(entity, parent, builder, info);
    // }

    public class FSharpCompiledDelegateFactory(FSharpMetadata metadata) : InterfaceFactory
    {
      public FSharpMetadata Metadata { get; } = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info) =>
        !Metadata.EntitiesByName.TryGetValue(info.FullyQualifiedName, out var entity)
          ? base.Create(parent, builder, info)
          : new FSharpCompiledDelegate(entity, parent, builder, info);
    }
  }
}
