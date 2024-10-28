using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  [PsiComponent(Instantiation.DemandAnyThreadUnsafe)]
  public class FSharpAutoOpenCache : IAssemblyCache
  {
    private readonly IPsiAssemblyFileLoader myPsiAssemblyFileLoader;

    private readonly Dictionary<IPsiModule, ISet<string>> myAssemblyAutoOpenModules = new();
    private readonly OneToSetMap<string, TypePart> mySourceModuleToNestedAutoOpenModules = new();
    private readonly OneToSetMap<string, string> myCompiledModuleToNestedAutoOpenModules = new();

    public FSharpAutoOpenCache(IPsiAssemblyFileLoader psiAssemblyFileLoader, ISymbolCache symbolCache)
    {
      myPsiAssemblyFileLoader = psiAssemblyFileLoader;

      symbolCache.OnAfterTypePartAdded += AfterTypePartAdded;
      symbolCache.OnBeforeTypePartRemoved += BeforeTypePartRemoved;
    }

    public ISet<string> GetAutoOpenedModules([NotNull] IPsiModule psiModule) =>
      myAssemblyAutoOpenModules.TryGetValue(psiModule, EmptySet<string>.Instance);

    private static string GetQualifiedName(IClrDeclaredElement declaredElement) =>
      declaredElement switch
      {
        INamespace ns => ns.QualifiedName,
        ITypeElement typeElement => typeElement.GetClrName().FullName,
        _ => null
      };

    public IEnumerable<ITypeElement> GetAutoImportedElements([CanBeNull] string qualifiedName,
      ISymbolScope symbolScope)
    {
      if (qualifiedName == null)
        return EmptyList<ITypeElement>.Instance;

      var result = new HashSet<ITypeElement>();

      foreach (var part in mySourceModuleToNestedAutoOpenModules.GetReadOnlyValues(qualifiedName))
      {
        var typeElement = part.TypeElement;
        if (typeElement != null && typeElement.IsValid() && symbolScope.Contains(typeElement))
          result.Add(typeElement);
      }

      foreach (var compiledTypeName in myCompiledModuleToNestedAutoOpenModules.GetReadOnlyValues(qualifiedName))
        if (symbolScope.GetTypeElementByCLRName(compiledTypeName) is { } compiledTypeElement)
          result.Add(compiledTypeElement);

      return result;
    }

    public IEnumerable<ITypeElement> GetAutoImportedElements([NotNull] IClrDeclaredElement declaredElement,
      ISymbolScope symbolScope)
    {
      var qualifiedName = GetQualifiedName(declaredElement);
      return GetAutoImportedElements(qualifiedName, symbolScope);
    }


    object ICache.Load(IProgressIndicator progress, bool enablePersistence) => null;

    void ICache.MergeLoaded(object data)
    {
    }

    void ICache.Save(IProgressIndicator progress, bool enablePersistence)
    {
    }

    private static bool IsApplicable(TypePart typePart) =>
      typePart.CanHaveAttribute(FSharpPredefinedType.AutoOpenAttrTypeName.ShortName);

    private IEnumerable<IClrDeclaredElement> GetSourceImportingParentModules([NotNull] TypeElement typeElement)
    {
      var containingType = typeElement.ContainingType;
      if (containingType == null)
      {
        yield return typeElement.GetContainingNamespace();
        yield break;
      }

      while (containingType != null)
      {
        yield return containingType;

        if (containingType.GetAccessType() == ModuleMembersAccessKind.AutoOpen)
        {
          containingType = containingType.GetContainingType();
          if (containingType == null)
            yield return typeElement.GetContainingNamespace();
        }

        else
          break;
      }
    }

    private IEnumerable<string> GetCompiledImportingParentModuleNames([NotNull] IMetadataTypeInfo typeInfo)
    {
      var containingType = typeInfo.DeclaringType;
      if (containingType == null)
      {
        yield return typeInfo.NamespaceName;
        yield break;
      }

      while (containingType != null)
      {
        yield return containingType.FullyQualifiedName;

        if (containingType.HasCustomAttribute(FSharpPredefinedType.AutoOpenAttrTypeName.FullName))
        {
          containingType = containingType.DeclaringType;
          if (containingType == null)
            yield return typeInfo.NamespaceName;
        }

        else
          break;
      }
    }


    private void AfterTypePartAdded(TypePart addedTypePart)
    {
      if (!IsApplicable(addedTypePart))
        return;

      var typeElement = addedTypePart.TypeElement;
      Assertion.AssertNotNull(typeElement);

      foreach (var parentModule in GetSourceImportingParentModules(typeElement))
        if (GetQualifiedName(parentModule) is { } qualifiedName)
          mySourceModuleToNestedAutoOpenModules.Add(qualifiedName, addedTypePart);
    }

    private void BeforeTypePartRemoved(TypePart removedTypePart)
    {
      if (!IsApplicable(removedTypePart))
        return;

      var typeElement = removedTypePart.TypeElement;
      Assertion.AssertNotNull(typeElement);

      foreach (var parentModule in GetSourceImportingParentModules(typeElement))
        if (GetQualifiedName(parentModule) is { } qualifiedName)
          mySourceModuleToNestedAutoOpenModules.Remove(qualifiedName, removedTypePart);
    }

    private static bool IsApplicable(IMetadataAssembly metadataAssembly) =>
      metadataAssembly.ReferencedAssembliesNames.Any(assemblyName => assemblyName.IsFSharpCore()) ||
      metadataAssembly.AssemblyName.IsFSharpCore();

    private ISet<string> Build(IMetadataAssembly metadataAssembly)
    {
      if (!IsApplicable(metadataAssembly))
        return null;

      var modulesNames = new HashSet<string>();

      var autoOpenAttributes = metadataAssembly.GetCustomAttributes(FSharpPredefinedType.AutoOpenAttrTypeName.FullName);
      foreach (var autoOpenAttribute in autoOpenAttributes)
      {
        var args = autoOpenAttribute.ConstructorArguments;
        if (args.Length != 1 || args[0].Value is not string moduleName)
          continue;

        modulesNames.Add(moduleName);
      }

      foreach (var typeInfo in metadataAssembly.GetTypes())
        if (typeInfo.HasCustomAttribute(FSharpPredefinedType.AutoOpenAttrTypeName.FullName))
        {
          foreach (var moduleName in GetCompiledImportingParentModuleNames(typeInfo))
            myCompiledModuleToNestedAutoOpenModules.Add(moduleName, typeInfo.FullyQualifiedName);

          modulesNames.Add(typeInfo.TypeName);
        }

      return modulesNames;
    }

    object IAssemblyCache.Build(IPsiAssembly assembly)
    {
      ISet<string> result = null;
      myPsiAssemblyFileLoader.GetOrLoadAssembly(assembly, true, (_, _, metadataAssembly) =>
        result = Build(metadataAssembly));

      return result;
    }

    void IAssemblyCache.Merge(IPsiAssembly assembly, object part, Func<bool> checkForTermination)
    {
      if (part is ISet<string> cache)
        myAssemblyAutoOpenModules.Add(assembly.PsiModule, cache);
    }

    void IAssemblyCache.Drop(IEnumerable<IPsiAssembly> assemblies)
    {
      foreach (var psiAssembly in assemblies)
        myAssemblyAutoOpenModules.Remove(psiAssembly.PsiModule);
    }
  }
}
