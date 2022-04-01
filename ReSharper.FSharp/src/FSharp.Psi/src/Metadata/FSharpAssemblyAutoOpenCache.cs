using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  [PsiComponent]
  public class FSharpAssemblyAutoOpenCache : IAssemblyCache
  {
    private readonly IPsiAssemblyFileLoader myPsiAssemblyFileLoader;

    private readonly IDictionary<IPsiModule, ISet<string>> myAutoOpenModules =
      new Dictionary<IPsiModule, ISet<string>>();

    public FSharpAssemblyAutoOpenCache(IPsiAssemblyFileLoader psiAssemblyFileLoader) =>
      myPsiAssemblyFileLoader = psiAssemblyFileLoader;

    public ISet<string> GetAutoOpenedModules([NotNull] IPsiModule psiModule) =>
      myAutoOpenModules.TryGetValue(psiModule, EmptySet<string>.Instance);

    object ICache.Load(IProgressIndicator progress, bool enablePersistence) => null;

    void ICache.MergeLoaded(object data)
    {
    }

    void ICache.Save(IProgressIndicator progress, bool enablePersistence)
    {
    }

    private static ISet<string> Build(IMetadataAssembly metadataAssembly)
    {
      var modules = new HashSet<string>();

      var autoOpenAttributes = metadataAssembly.GetCustomAttributes(FSharpPredefinedType.AutoOpenAttrTypeName.FullName);
      foreach (var autoOpenAttribute in autoOpenAttributes)
      {
        var args = autoOpenAttribute.ConstructorArguments;
        if (args.Length == 1 && args[0].Value is string module)
          modules.Add(module);
      }

      return modules;
    }

    object IAssemblyCache.Build(IPsiAssembly assembly)
    {
      ISet<string> result = null;
      myPsiAssemblyFileLoader.GetOrLoadAssembly(assembly, true, (_, _, metadataAssembly) =>
        result = Build(metadataAssembly));

      return result;
    }

    void IAssemblyCache.Merge(IPsiAssembly assembly, object part, Func<bool> checkForTermination) =>
      myAutoOpenModules.Add(assembly.PsiModule, (ISet<string>) part);

    void IAssemblyCache.Drop(IEnumerable<IPsiAssembly> assemblies)
    {
      foreach (var psiAssembly in assemblies)
        myAutoOpenModules.Remove(psiAssembly.PsiModule);
    }
  }
}
