using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public interface IUnitOfWork
  {
    IOutOfProcessHostFactory<RdFSharpTypeProvidersLoaderModel> TypeProvidersLoaderHostFactory { get; }
    IOutOfProcessHostFactory<RdTypeProviderProcessModel> TypeProvidersHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedNamespaceProcessModel> ProvidedNamespacesHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedTypeProcessModel> ProvidedTypesHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedPropertyInfoProcessModel> ProvidedPropertyInfosHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedMethodInfoProcessModel> ProvidedMethodInfosHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedParameterInfoProcessModel> ProvidedParameterInfosHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedAssemblyProcessModel> ProvidedAssemblyHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedFieldInfoProcessModel> ProvidedFieldInfosHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedEventInfoProcessModel> ProvidedEventInfosHostFactory { get; }
    IOutOfProcessHostFactory<RdProvidedConstructorInfoProcessModel> ProvidedConstructorInfosHostFactory { get; }
  }

  public class UnitOfWork : IUnitOfWork
  {
    public IOutOfProcessHostFactory<RdFSharpTypeProvidersLoaderModel> TypeProvidersLoaderHostFactory { get; }
    public IOutOfProcessHostFactory<RdTypeProviderProcessModel> TypeProvidersHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedNamespaceProcessModel> ProvidedNamespacesHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedTypeProcessModel> ProvidedTypesHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedPropertyInfoProcessModel> ProvidedPropertyInfosHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedMethodInfoProcessModel> ProvidedMethodInfosHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedParameterInfoProcessModel> ProvidedParameterInfosHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedAssemblyProcessModel> ProvidedAssemblyHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedFieldInfoProcessModel> ProvidedFieldInfosHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedEventInfoProcessModel> ProvidedEventInfosHostFactory { get; }
    public IOutOfProcessHostFactory<RdProvidedConstructorInfoProcessModel> ProvidedConstructorInfosHostFactory { get; }

    public UnitOfWork()
    {
      var typeProvidersLoader = new TypeProvidersLoader();

      var typeProvidersCache = new ProvidedCacheBase<ITypeProvider>();
      var typeProviderRdModelsCreator = new TypeProviderRdModelsCreator(typeProvidersCache);

      var providedTypesCache = new ProvidedCacheBase<Tuple<ProvidedType, RdProvidedType, int>>();
      var providedTypeRdModelsCreator =
        new ProvidedTypeRdModelsCreator(providedTypesCache, new ProvidedTypeEqualityComparer());

      var providedNamespacesCache = new ProvidedCacheBase<Tuple<IProvidedNamespace, int>>();
      var providedNamespaceRdModelsCreator = new ProvidedNamespaceRdModelCreator(providedNamespacesCache);

      var providedParameterInfosCache = new ProvidedCacheBase<Tuple<ProvidedParameterInfo, int>>();
      var providedParameterInfoRdModelsCreator = new ProvidedParameterInfoRdModelsCreator(providedParameterInfosCache);

      var providedPropertyInfosCache = new ProvidedCacheBase<Tuple<ProvidedPropertyInfo, int>>();
      var providedPropertyInfoRdModelsCreator = new ProvidedPropertyInfoRdModelsCreator(providedPropertyInfosCache);

      var providedMethodInfosCache = new ProvidedCacheBase<Tuple<ProvidedMethodInfo, int>>();
      var providedMethodInfoRdModelsCreator = new ProvidedMethodInfoRdModelsCreator(providedMethodInfosCache);

      var providedAssembliesCache = new ProvidedCacheBase<ProvidedAssembly>();
      var providedAssemblyRdModelsCreator = new ProvidedAssemblyRdModelCreator(providedAssembliesCache);

      var providedFieldInfosCache = new ProvidedCacheBase<Tuple<ProvidedFieldInfo, int>>();
      var providedFieldInfoRdModelsCreator = new ProvidedFieldInfoRdModelCreator(providedFieldInfosCache);

      var providedEventInfosCache = new ProvidedCacheBase<Tuple<ProvidedEventInfo, int>>();
      var providedEventInfoRdModelsCreator = new ProvidedEventInfoRdModelsCreator(providedEventInfosCache);

      var providedConstructorInfosCache = new ProvidedCacheBase<Tuple<ProvidedConstructorInfo, int>>();
      var providedConstructorInfoRdModelsCreator =
        new ProvidedConstructorInfoRdModelsCreator(providedConstructorInfosCache);

      TypeProvidersLoaderHostFactory =
        new TypeProvidersLoaderHostFactory(typeProvidersLoader, typeProviderRdModelsCreator);
      TypeProvidersHostFactory =
        new TypeProvidersHostFactory(providedTypesCache, typeProvidersCache, providedNamespaceRdModelsCreator);
      ProvidedNamespacesHostFactory =
        new ProvidedNamespacesHostFactory(providedNamespacesCache, providedNamespaceRdModelsCreator,
          providedTypeRdModelsCreator);
      ProvidedTypesHostFactory = new ProvidedTypesHostFactory(providedParameterInfoRdModelsCreator,
        providedMethodInfoRdModelsCreator, providedPropertyInfoRdModelsCreator, providedTypeRdModelsCreator,
        providedFieldInfoRdModelsCreator, providedEventInfoRdModelsCreator, providedAssemblyRdModelsCreator,
        providedConstructorInfoRdModelsCreator, providedTypesCache, typeProvidersCache);
      ProvidedPropertyInfosHostFactory =
        new ProvidedPropertyInfoHostFactory(providedParameterInfoRdModelsCreator, providedTypeRdModelsCreator,
          providedMethodInfoRdModelsCreator, providedPropertyInfosCache);
      ProvidedMethodInfosHostFactory = new ProvidedMethodInfosHostFactory(providedTypeRdModelsCreator,
        providedParameterInfoRdModelsCreator, providedMethodInfosCache);
      ProvidedParameterInfosHostFactory =
        new ProvidedParameterInfosHostFactory(providedTypeRdModelsCreator, providedParameterInfosCache);
      ProvidedAssemblyHostFactory = new ProvidedAssemblyHostFactory(providedAssembliesCache, typeProvidersCache);
      ProvidedFieldInfosHostFactory =
        new ProvidedFieldInfoHostFactory(providedFieldInfosCache, providedTypeRdModelsCreator);
      ProvidedEventInfosHostFactory = new ProvidedEventInfoHostFactory(providedTypeRdModelsCreator,
        providedMethodInfoRdModelsCreator, providedEventInfosCache);

      ProvidedConstructorInfosHostFactory = new ProvidedConstructorInfosHostFactory(providedTypeRdModelsCreator,
        providedParameterInfoRdModelsCreator, providedConstructorInfosCache, typeProvidersCache);
    }
  }
}
