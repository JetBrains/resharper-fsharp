using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Core.CompilerServices;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyTypeProviderWithCache : IProxyTypeProvider, IRdProvidedEntity
  {
    private readonly RdTypeProvider myRdTypeProvider;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private RdTypeProviderProcessModel RdTypeProviderProcessModel => myProcessModel.RdTypeProviderProcessModel;
    public int EntityId => myRdTypeProvider.EntityId;

    public ProxyTypeProviderWithCache(
      RdTypeProvider rdTypeProvider,
      IProvidedTypesCache cache,
      RdFSharpTypeProvidersLoaderModel processModel)
    {
      myRdTypeProvider = rdTypeProvider;
      myProcessModel = processModel;

      myCache = cache;
      InitCaches();

      var lifeTime = new Lifetime(); //temp
      RdTypeProviderProcessModel.Invalidate.Advise(lifeTime, OnInvalidate);
    }

    private void OnInvalidate(int typeProviderId)
    {
      if (typeProviderId != EntityId) return;

      InitCaches();
      Invalidate?.Invoke(this, EventArgs.Empty);
    }

    public IProvidedNamespace[] GetNamespaces() => myProvidedNamespaces.Value;

    public ParameterInfo[] GetStaticParameters(Type typeWithoutArguments) =>
      throw new Exception("GetStaticParameters should be unreachable");

    public Type ApplyStaticArguments(Type typeWithoutArguments, string[] typePathWithArguments,
      object[] staticArguments) =>
      throw new Exception("ApplyStaticArguments should be unreachable");

    public FSharpExpr GetInvokerExpression(MethodBase syntheticMethodBase, FSharpExpr[] parameters) =>
      throw new Exception("GetInvokerExpression should be unreachable");

    public byte[] GetGeneratedAssemblyContents(Assembly assembly) =>
      throw new Exception("GetGeneratedAssemblyContents should be unreachable");

    public ParameterInfo[] GetStaticParametersForMethod(MethodBase methodWithoutArguments) =>
      throw new Exception("GetStaticParametersForMethod should be unreachable");

    public MethodBase ApplyStaticArgumentsForMethod(MethodBase methodWithoutArguments, string methodNameWithArguments,
      object[] staticArguments) =>
      throw new Exception("ApplyStaticArgumentsForMethod should be unreachable");

    public ProvidedExpr GetInvokerExpression(ProvidedMethodBase methodBase, ProvidedVar[] paramExprs)
    {
      var providedMethodBase = methodBase as IProxyProvidedNamedEntity;
      var providedVarParamExprs = paramExprs.Select(x => x as IProxyProvidedNamedEntity).ToArray();

      Assertion.Assert(providedMethodBase != null, "methodBase is not ProxyProvided");
      Assertion.Assert(providedVarParamExprs.All(x => x != null), "paramExprs is not ProxyProvided");

      var key = string.Concat(providedMethodBase.FullName, providedVarParamExprs.Select(t => t.FullName));
      if (!myInvokerExpressionsCache.TryGetValue(key, out var expr))
      {
        var providedMethodBaseId = providedMethodBase.EntityId;
        var providedVarParamExprIds = providedVarParamExprs.Select(x => x.EntityId).ToArray();

        expr = ProxyProvidedExprWithCache.Create(RdTypeProviderProcessModel.GetInvokerExpression.Sync(
            new GetInvokerExpressionArgs(EntityId, methodBase is ProvidedConstructorInfo, providedMethodBaseId,
              providedVarParamExprIds)), EntityId,
          myProcessModel, methodBase.Context, myCache);

        myInvokerExpressionsCache.Add(key, expr);
      }

      return expr;
    }

    public string GetDisplayName(bool fullName) => fullName ? myRdTypeProvider.FullName : myRdTypeProvider.Name;

    public void Dispose() => RdTypeProviderProcessModel.Dispose.Sync(EntityId);

    private void InitCaches()
    {
      // ReSharper disable once CoVariantArrayConversion
      myProvidedNamespaces = new InterruptibleLazy<IProvidedNamespace[]>(() => RdTypeProviderProcessModel.GetNamespaces
        .Sync(EntityId)
        .Select(t => new ProxyProvidedNamespaceWithCache(t, EntityId, myProcessModel, myCache))
        .ToArray());

      myInvokerExpressionsCache = new Dictionary<string, ProxyProvidedExprWithCache>();
      myCache.Invalidate(EntityId);
    }

    public event EventHandler Invalidate;

    private readonly IProvidedTypesCache myCache;
    private InterruptibleLazy<IProvidedNamespace[]> myProvidedNamespaces;
    private Dictionary<string, ProxyProvidedExprWithCache> myInvokerExpressionsCache;
  }
}
