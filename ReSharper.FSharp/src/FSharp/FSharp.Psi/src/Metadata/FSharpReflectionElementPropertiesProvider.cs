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
      DelegateProperties = new FSharpCompiledDelegateFactory(metadata);
      // EnumProperties = new FSharpCompiledEnumFactory(metadata);
      InterfaceProperties = new FSharpCompiledInterfaceFactory(metadata);
      StructProperties = new FSharpCompiledStructFactory(metadata);
    }

    public override CompiledTypeElementFactory ClassProperties { get; }
    public override CompiledTypeElementFactory DelegateProperties { get; }
    // public override CompiledTypeElementFactory EnumProperties { get; }
    public override CompiledTypeElementFactory InterfaceProperties { get; }
    public override CompiledTypeElementFactory StructProperties { get; }

    public class FSharpCompiledClassFactory : ClassFactory
    {
      public FSharpMetadata Metadata { get; }

      public FSharpCompiledClassFactory(FSharpMetadata metadata) =>
        Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info)
      {
        if (!Metadata.Entities.TryGetValue(info.FullyQualifiedName, out var entity))
          return base.Create(parent, builder, info);

        return entity.Representation.IsModule
          ? new FSharpCompiledModule(entity, parent, builder, info)
          : new FSharpCompiledClass(entity, parent, builder, info);
      }
    }

    public class FSharpCompiledStructFactory : StructFactory
    {
      public FSharpMetadata Metadata { get; }

      public FSharpCompiledStructFactory(FSharpMetadata metadata) =>
        Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info) =>
        !Metadata.Entities.TryGetValue(info.FullyQualifiedName, out var entity)
          ? base.Create(parent, builder, info)
          : new FSharpCompiledStruct(entity, parent, builder, info);
    }

    public class FSharpCompiledInterfaceFactory : InterfaceFactory
    {
      public FSharpMetadata Metadata { get; }

      public FSharpCompiledInterfaceFactory(FSharpMetadata metadata) =>
        Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info) =>
        !Metadata.Entities.TryGetValue(info.FullyQualifiedName, out var entity)
          ? base.Create(parent, builder, info)
          : new FSharpCompiledInterface(entity, parent, builder, info);
    }

    // public class FSharpCompiledEnumFactory : InterfaceFactory
    // {
    //   public FSharpMetadata Metadata { get; }
    //
    //   public FSharpCompiledEnumFactory(FSharpMetadata metadata) =>
    //     Metadata = metadata;
    //
    //   public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info) =>
    //     !Metadata.Entities.TryGetValue(info.FullyQualifiedName, out var entity)
    //       ? base.Create(parent, builder, info)
    //       : new FSharpCompiledInterface(entity, parent, builder, info);
    // }

    public class FSharpCompiledDelegateFactory : InterfaceFactory
    {
      public FSharpMetadata Metadata { get; }

      public FSharpCompiledDelegateFactory(FSharpMetadata metadata) =>
        Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder, IMetadataTypeInfo info) =>
        !Metadata.Entities.TryGetValue(info.FullyQualifiedName, out var entity)
          ? base.Create(parent, builder, info)
          : new FSharpCompiledDelegate(entity, parent, builder, info);
    }
  }
}
