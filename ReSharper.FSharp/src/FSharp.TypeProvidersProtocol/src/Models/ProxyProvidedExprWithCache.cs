using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedExprWithCache : ProvidedExpr, IRdProvidedEntity
  {
    private readonly RdProvidedExpr myExpr;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly ITypeProviderCache myCache;
    public int EntityId => myExpr.EntityId;
    private RdProvidedExprProcessModel RdProvidedExprProcessModel => myProcessModel.RdProvidedExprProcessModel;

    public ProxyProvidedExprWithCache(RdProvidedExpr expr, RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, ITypeProviderCache cache) : base(FSharpExpr.Value(0), context)
    {
      myExpr = expr;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;
    }

    [ContractAnnotation("expr:null => null")]
    public static ProxyProvidedExprWithCache Create(RdProvidedExpr expr,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      expr == null ? null : new ProxyProvidedExprWithCache(expr, processModel, context, cache);

    public override string UnderlyingExpressionString => myExpr.UnderlyingExpressionString;

    public override ProvidedType Type =>
      myCache.GetOrCreateWithContext(myTypeId ??= RdProvidedExprProcessModel.GetType.Sync(EntityId), myContext);

    
    public override FSharpOption<ProvidedExprType> GetExprType()
    {
      throw new NotImplementedException("IS THIS GetExprTypeCall?");
    }

    private int? myTypeId;
  }
}
