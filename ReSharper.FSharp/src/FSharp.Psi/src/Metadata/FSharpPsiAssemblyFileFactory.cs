using System;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.AssemblyFileLoaderZoned;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util.Caches;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  [PsiComponent]
  public class FSharpPsiAssemblyFileFactory : IPsiAssemblyFileFactory
  {
    public int Priority => 20;

    private static bool IsFSharpMetadataResource(IMetadataManifestResource resource) =>
      resource.Name.StartsWith("FSharpSignatureInfo.", StringComparison.Ordinal) ||
      resource.Name.StartsWith("FSharpSignatureData.", StringComparison.Ordinal);

    private static bool IsFSharpSignatureAttribute(MetadataTypeReference typeReference) =>
      typeReference.FullName.Equals(FSharpAssemblyUtil.InterfaceDataVersionAttrConcatTypeName);

    public bool IsApplicable(IPsiAssembly assembly, IMetadataAssembly metadataAssembly)
    {
      foreach (var resource in metadataAssembly.GetManifestResources())
      {
        if (!IsFSharpMetadataResource(resource)) continue;

        foreach (var typeReference in metadataAssembly.CustomAttributesTypeNames)
          if (IsFSharpSignatureAttribute(typeReference))
            return true;
      }

      return false;
    }

    public AssemblyPsiFile CreateFile(Func<IAssemblyLocation, IPsiModule, MetadataLoader> metadataLoaderFactory,
      IPsiConfiguration psiConfiguration, IExternalProviderCache<ICompiledEntity, IType> decodedTypeCache,
      IWeakRefRetainerCache<object> compiledMembersBucketCache) =>
      new FSharpAssemblyPsiFile(metadataLoaderFactory, psiConfiguration, decodedTypeCache, compiledMembersBucketCache);
  }
}
