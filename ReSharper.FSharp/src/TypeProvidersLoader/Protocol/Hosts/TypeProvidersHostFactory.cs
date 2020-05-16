using System;
using System.Linq;
using System.Reflection;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class TypeProvidersHostFactory : IOutOfProcessHostFactory<RdTypeProviderProcessModel>
  {
    private readonly IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> myProvidedTypesCache;
    private readonly IReadProvidedCache<ITypeProvider> myTypeProvidersCache;
    private readonly IReadProvidedCache<Tuple<ProvidedVar, int>> myProvidedVarsCache;
    private readonly IReadProvidedCache<Tuple<ProvidedMethodInfo, int>> myProvidedMethodsCache;
    private readonly IReadProvidedCache<Tuple<ProvidedConstructorInfo, int>> myProvidedConstructorsCache;
    private readonly IProvidedRdModelsCreator<IProvidedNamespace, RdProvidedNamespace> myProvidedNamespacesCreator;
    private readonly IProvidedRdModelsCreator<ProvidedExpr, RdProvidedExpr> myProvidedExprsCreator;

    public TypeProvidersHostFactory(IReadProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> providedTypesCache,
      IReadProvidedCache<ITypeProvider> typeProvidersCache,
      IReadProvidedCache<Tuple<ProvidedVar, int>> providedVarsCache,
      IReadProvidedCache<Tuple<ProvidedMethodInfo, int>> providedMethodsCache,
      IReadProvidedCache<Tuple<ProvidedConstructorInfo, int>> providedConstructorsCache,
      IProvidedRdModelsCreator<IProvidedNamespace, RdProvidedNamespace> providedNamespacesCreator,
      IProvidedRdModelsCreator<ProvidedExpr, RdProvidedExpr> providedExprsCreator)
    {
      myProvidedTypesCache = providedTypesCache;
      myTypeProvidersCache = typeProvidersCache;
      myProvidedVarsCache = providedVarsCache;
      myProvidedMethodsCache = providedMethodsCache;
      myProvidedConstructorsCache = providedConstructorsCache;
      myProvidedNamespacesCreator = providedNamespacesCreator;
      myProvidedExprsCreator = providedExprsCreator;
    }

    public void Initialize(RdTypeProviderProcessModel processModel)
    {
      processModel.GetNamespaces.Set(GetTypeProviderNamespaces);
      processModel.GetProvidedType.Set(GetProvidedType);
      processModel.GetInvokerExpression.Set(GetInvokerExpression);
    }

    private RdProvidedExpr GetInvokerExpression(GetInvokerExpressionArgs args)
    {
      var typeProvider = myTypeProvidersCache.Get(args.TypeProviderId);

      //TODO: Rewrite it better?
      var method = args.IsConstructor
        ? (MethodBase) myProvidedConstructorsCache.Get(args.ProvidedMethodBaseId).Item1.Handle
        : myProvidedMethodsCache.Get(args.ProvidedMethodBaseId).Item1.Handle;

      var vars = args.ProvidedVarParamExprIds
        .Select(myProvidedVarsCache.Get)
        .Select(t => FSharpExpr.Var(t.Item1.Handle))
        .ToArray();

      return myProvidedExprsCreator.CreateRdModel(
        new ProvidedExpr(typeProvider.GetInvokerExpression(method, vars), ProvidedTypeContext.Empty),
        args.TypeProviderId);
    }

    private RdProvidedType GetProvidedType(GetProvidedTypeArgs args)
    {
      var (_, type, _) = myProvidedTypesCache.Get(args.Id);
      return type;
    }

    private RdProvidedNamespace[] GetTypeProviderNamespaces(int providerId)
    {
      var typeProvider = myTypeProvidersCache.Get(providerId);
      return typeProvider
        .GetNamespaces()
        .CreateRdModels(myProvidedNamespacesCreator, providerId);
    }
  }
}
