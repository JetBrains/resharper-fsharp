using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util.Caches;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpAssemblyPsiFile : AssemblyPsiFile
  {
    public FSharpMetadata FSharpMetadata { get; set; }

    public FSharpAssemblyPsiFile([NotNull] Func<IAssemblyLocation, IPsiModule, MetadataLoader> metadataLoaderFactory,
      [NotNull] IPsiConfiguration psiConfiguration,
      [NotNull] IExternalProviderCache<ICompiledEntity, IType> decodedTypeCache,
      [NotNull] IWeakRefRetainerCache<object> compiledMembersBucketCache)
      : base(metadataLoaderFactory, psiConfiguration, decodedTypeCache, compiledMembersBucketCache)
    {
    }

    public override void LoadAssembly(IMetadataAssembly assembly, IAssemblyPsiModule containingModule)
    {
      FSharpMetadata = FSharpMetadataReader.ReadMetadata(containingModule, assembly);
      base.LoadAssembly(assembly, containingModule);
      FSharpMetadata = null;
    }

    protected override ReflectionTypeBuilder CreateReflectionTypeBuilder(IAssemblyDataCache cache, IMetadataAssembly assembly)
    {
      return base.CreateReflectionTypeBuilder(cache, assembly);
    }

    protected override void LoadMetadataErasedTypes(IMetadataAssembly metadataAssembly, List<CompiledTypeElement> loadedTypes)
    {
      using var builder = GetReflectionBuilder(metadataAssembly);
    }

    protected override ReflectionElementPropertiesProvider CreateReflectionElementPropertiesProvider() =>
      new FSharpReflectionElementPropertiesProvider(FSharpMetadata);
  }
}
