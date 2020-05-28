using System;
using System.Linq;
using System.Reflection;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class TypeProvidersHostFactory : IOutOfProcessHostFactory<RdTypeProviderProcessModel>
  {
    private readonly UnitOfWork myUnitOfWork;

    public TypeProvidersHostFactory(UnitOfWork unitOfWork)
    {
      myUnitOfWork = unitOfWork;
    }

    public void Initialize(RdTypeProviderProcessModel processModel)
    {
      processModel.GetNamespaces.Set(GetTypeProviderNamespaces);
      processModel.GetProvidedType.Set(GetProvidedType);
      processModel.GetInvokerExpression.Set(GetInvokerExpression);
    }

    private RdProvidedExpr GetInvokerExpression(GetInvokerExpressionArgs args)
    {
      var typeProvider = myUnitOfWork.TypeProvidersCache.Get(args.TypeProviderId);

      //TODO: Rewrite it better?
      var method = args.IsConstructor
        ? (MethodBase) myUnitOfWork.ProvidedConstructorInfosCache.Get(args.ProvidedMethodBaseId).Item1.Handle
        : myUnitOfWork.ProvidedMethodInfosCache.Get(args.ProvidedMethodBaseId).Item1.Handle;

      var vars = args.ProvidedVarParamExprIds
        .Select(myUnitOfWork.ProvidedVarsCache.Get)
        .Select(t => FSharpExpr.Var(t.Item1.Handle))
        .ToArray();

      return myUnitOfWork.ProvidedExprRdModelsCreator.CreateRdModel(
        new ProvidedExpr(typeProvider.GetInvokerExpression(method, vars), ProvidedTypeContext.Empty),
        args.TypeProviderId);
    }

    private RdProvidedType GetProvidedType(GetProvidedTypeArgs args)
    {
      var (_, type, _) = myUnitOfWork.ProvidedTypesCache.Get(args.Id);
      return type;
    }

    private RdProvidedNamespace[] GetTypeProviderNamespaces(int providerId)
    {
      var typeProvider = myUnitOfWork.TypeProvidersCache.Get(providerId);
      return typeProvider
        .GetNamespaces()
        .CreateRdModels(myUnitOfWork.ProvidedNamespaceRdModelsCreator, providerId);
    }
  }
}
