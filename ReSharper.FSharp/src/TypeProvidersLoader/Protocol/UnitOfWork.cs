using System;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class UnitOfWork
  {
    public ITypeProvidersLoader TypeProvidersLoader { get; }
    public IProvidedCache<ITypeProvider> TypeProvidersCache { get; }
    public IProvidedCache<Tuple<ProvidedVar, int>> ProvidedVarsCache { get; }
    public IProvidedCache<Tuple<ProvidedExpr, int>> ProvidedExprsCache { get; }
    public IProvidedCache<Tuple<ProvidedConstructorInfo, int>> ProvidedConstructorInfosCache { get; }
    public IProvidedCache<Tuple<ProvidedFieldInfo, int>> ProvidedFieldInfosCache { get; }
    public IProvidedCache<Tuple<ProvidedAssembly, int>> ProvidedAssembliesCache { get; }
    public IProvidedCache<Tuple<ProvidedMethodInfo, int>> ProvidedMethodInfosCache { get; }
    public IProvidedCache<Tuple<ProvidedPropertyInfo, int>> ProvidedPropertyInfosCache { get; }
    public IProvidedCache<Tuple<ProvidedParameterInfo, int>> ProvidedParameterInfosCache { get; }
    public IProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> ProvidedTypesCache { get; }
    public IProvidedCache<Tuple<IProvidedNamespace, int>> ProvidedNamespacesCache { get; }
    public IProvidedCache<Tuple<ProvidedEventInfo, int>> ProvidedEventInfosCache { get; }

    public IProvidedRdModelsCreator<ITypeProvider, RdTypeProvider> TypeProviderRdModelsCreator { get; }
    public IProvidedRdModelsCreator<ProvidedType, RdProvidedType> ProvidedTypeRdModelsCreator { get; }
    public IProvidedRdModelsCreator<IProvidedNamespace, RdProvidedNamespace> ProvidedNamespaceRdModelsCreator { get; }

    public IProvidedRdModelsCreator<ProvidedParameterInfo, RdProvidedParameterInfo>
      ProvidedParameterInfoRdModelsCreator { get; }

    public IProvidedRdModelsCreator<ProvidedPropertyInfo, RdProvidedPropertyInfo>
      ProvidedPropertyInfoRdModelsCreator { get; }

    public IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> ProvidedMethodInfoRdModelsCreator { get; }
    public IProvidedRdModelsCreator<ProvidedAssembly, RdProvidedAssembly> ProvidedAssemblyRdModelsCreator { get; }
    public IProvidedRdModelsCreator<ProvidedFieldInfo, RdProvidedFieldInfo> ProvidedFieldInfoRdModelsCreator { get; }
    public IProvidedRdModelsCreator<ProvidedEventInfo, RdProvidedEventInfo> ProvidedEventInfoRdModelsCreator { get; }

    public IProvidedRdModelsCreator<ProvidedConstructorInfo, RdProvidedConstructorInfo>
      ProvidedConstructorInfoRdModelsCreator { get; }

    public IProvidedRdModelsCreator<ProvidedExpr, RdProvidedExpr> ProvidedExprRdModelsCreator { get; }
    public IProvidedRdModelsCreator<ProvidedVar, RdProvidedVar> ProvidedVarRdModelsCreator { get; }

    public UnitOfWork()
    {
      TypeProvidersLoader = new TypeProvidersLoader();

      TypeProvidersCache = new TypeProviderCache();
      ProvidedNamespacesCache = new SimpleProvidedCache<IProvidedNamespace>();
      ProvidedTypesCache = new ProvidedEntitiesWithRdModelsCache<ProvidedType, RdProvidedType>();
      ProvidedParameterInfosCache = new SimpleProvidedCache<ProvidedParameterInfo>();
      ProvidedPropertyInfosCache = new SimpleProvidedCache<ProvidedPropertyInfo>();
      ProvidedMethodInfosCache = new SimpleProvidedCache<ProvidedMethodInfo>();
      ProvidedAssembliesCache = new SimpleProvidedCache<ProvidedAssembly>();
      ProvidedFieldInfosCache = new SimpleProvidedCache<ProvidedFieldInfo>();
      ProvidedEventInfosCache = new SimpleProvidedCache<ProvidedEventInfo>();
      ProvidedConstructorInfosCache = new SimpleProvidedCache<ProvidedConstructorInfo>();
      ProvidedExprsCache = new SimpleProvidedCache<ProvidedExpr>();
      ProvidedVarsCache = new SimpleProvidedCache<ProvidedVar>();

      TypeProviderRdModelsCreator = new TypeProviderRdModelsCreator(TypeProvidersCache);
      ProvidedTypeRdModelsCreator =
        new ProvidedTypeRdModelsCreator(ProvidedTypesCache, new ProvidedTypeEqualityComparer());
      ProvidedNamespaceRdModelsCreator = new ProvidedNamespaceRdModelCreator(ProvidedNamespacesCache);
      ProvidedParameterInfoRdModelsCreator = new ProvidedParameterInfoRdModelsCreator(ProvidedParameterInfosCache);
      ProvidedPropertyInfoRdModelsCreator = new ProvidedPropertyInfoRdModelsCreator(ProvidedPropertyInfosCache);
      ProvidedMethodInfoRdModelsCreator = new ProvidedMethodInfoRdModelsCreator(ProvidedMethodInfosCache);
      ProvidedAssemblyRdModelsCreator = new ProvidedAssemblyRdModelCreator(ProvidedAssembliesCache);
      ProvidedFieldInfoRdModelsCreator = new ProvidedFieldInfoRdModelCreator(ProvidedFieldInfosCache);
      ProvidedEventInfoRdModelsCreator = new ProvidedEventInfoRdModelsCreator(ProvidedEventInfosCache);
      ProvidedConstructorInfoRdModelsCreator =
        new ProvidedConstructorInfoRdModelsCreator(ProvidedConstructorInfosCache);
      ProvidedExprRdModelsCreator = new ProvidedExprRdModelsCreator(ProvidedExprsCache);
      ProvidedVarRdModelsCreator = new ProvidedVarRdModelsCreator(ProvidedVarsCache);
    }
  }
}
