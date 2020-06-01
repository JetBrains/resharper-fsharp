using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedExprWithCache : ProvidedExpr, IRdProvidedEntity
  {
    private readonly RdProvidedExpr myExpr;
    private readonly int myTypeProviderId;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private readonly IProvidedTypesCache myCache;
    public int EntityId => myExpr.EntityId;
    private RdProvidedExprProcessModel RdProvidedExprProcessModel => myProcessModel.RdProvidedExprProcessModel;

    private ProxyProvidedExprWithCache(RdProvidedExpr expr, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, IProvidedTypesCache cache) : base(FSharpExpr.Value(0), context)
    {
      myExpr = expr;
      myTypeProviderId = typeProviderId;
      myProcessModel = processModel;
      myContext = context;
      myCache = cache;

      myExprType = new InterruptibleLazy<FSharpOption<ProvidedExprType>>(GetExprTypeInternal);
    }

    [ContractAnnotation("expr:null => null")]
    public static ProxyProvidedExprWithCache Create(RdProvidedExpr expr, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, IProvidedTypesCache cache) =>
      expr == null ? null : new ProxyProvidedExprWithCache(expr, typeProviderId, processModel, context, cache);

    public override string UnderlyingExpressionString => myExpr.UnderlyingExpressionString;

    public override ProvidedType Type =>
      myCache.GetOrCreateWithContext(myTypeId ??= RdProvidedExprProcessModel.Type.Sync(EntityId), myTypeProviderId,
        myContext);

    public override FSharpOption<ProvidedExprType> GetExprType() => myExprType.Value;

    private FSharpOption<ProvidedExprType> GetExprTypeInternal()
    {
      var data = RdProvidedExprProcessModel.GetExprType.Sync(EntityId);

      if (!data.HasValue) return FSharpOption<ProvidedExprType>.None;

      if (data.ProvidedNewArrayExpr != null)
      {
        var providedNewArrayExpr = data.ProvidedNewArrayExpr;
        var providedType =
          myCache.GetOrCreateWithContext(providedNewArrayExpr.ProvidedType, myTypeProviderId, myContext);
        var providedExprs = providedNewArrayExpr.ProvidedExprs
          .Select(t => Create(t, myTypeProviderId, myProcessModel, myContext, myCache))
          .ToArray();
        // ReSharper disable once CoVariantArrayConversion
        return ProvidedExprType.NewProvidedNewArrayExpr(providedType, providedExprs);
      }

      else if (data.ProvidedNewObjectExpr != null)
      {
        var providedNewObjectExpr = data.ProvidedNewObjectExpr;
        var providedConstructorInfo =
          ProxyProvidedConstructorInfoWithCache.Create(myTypeProviderId, providedNewObjectExpr.ProvidedConstructorInfo,
            myProcessModel,
            myContext, myCache);
        var providedExprs = providedNewObjectExpr.ProvidedExprs
          .Select(t => Create(t, myTypeProviderId, myProcessModel, myContext, myCache))
          .ToArray();
        // ReSharper disable once CoVariantArrayConversion
        return ProvidedExprType.NewProvidedNewObjectExpr(providedConstructorInfo, providedExprs);
      }

      else if (data.ProvidedWhileLoopExpr != null)
      {
        var providedWhileLoopExpr = data.ProvidedWhileLoopExpr;
        var providedExpr1 = Create(providedWhileLoopExpr.ProvidedExpr1, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var providedExpr2 = Create(providedWhileLoopExpr.ProvidedExpr2, myTypeProviderId, myProcessModel, myContext,
          myCache);
        return ProvidedExprType.NewProvidedWhileLoopExpr(providedExpr1, providedExpr2);
      }

      else if (data.ProvidedNewDelegateExpr != null)
      {
        var providedNewDelegateExpr = data.ProvidedNewDelegateExpr;
        var providedType =
          myCache.GetOrCreateWithContext(providedNewDelegateExpr.ProvidedType, myTypeProviderId, myContext);
        var providedVars = providedNewDelegateExpr.ProvidedVars
          .Select(t => ProxyProvidedVarWithCache.Create(t, myTypeProviderId, myProcessModel, myContext, myCache))
          .ToArray();
        var providedExpr = Create(providedNewDelegateExpr.ProvidedExpr, myTypeProviderId, myProcessModel, myContext,
          myCache);
        // ReSharper disable once CoVariantArrayConversion
        return ProvidedExprType.NewProvidedNewDelegateExpr(providedType, providedVars, providedExpr);
      }

      else if (data.ProvidedForIntegerRangeLoopExpr != null)
      {
        var providedForIntegerRangeLoopExpr = data.ProvidedForIntegerRangeLoopExpr;
        var providedVar = ProxyProvidedVarWithCache.Create(providedForIntegerRangeLoopExpr.ProvidedVar,
          myTypeProviderId, myProcessModel, myContext, myCache);
        var providedExpr1 = Create(providedForIntegerRangeLoopExpr.ProvidedExpr1, myTypeProviderId, myProcessModel,
          myContext, myCache);
        var providedExpr2 = Create(providedForIntegerRangeLoopExpr.ProvidedExpr2, myTypeProviderId, myProcessModel,
          myContext, myCache);
        var providedExpr3 = Create(providedForIntegerRangeLoopExpr.ProvidedExpr3, myTypeProviderId, myProcessModel,
          myContext, myCache);
        return ProvidedExprType.NewProvidedForIntegerRangeLoopExpr(providedVar, providedExpr1, providedExpr2,
          providedExpr3);
      }

      else if (data.ProvidedSequentialExpr != null)
      {
        var providedSequentialExpr = data.ProvidedSequentialExpr;
        var providedExpr1 = Create(providedSequentialExpr.ProvidedExpr1, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var providedExpr2 = Create(providedSequentialExpr.ProvidedExpr2, myTypeProviderId, myProcessModel, myContext,
          myCache);
        return ProvidedExprType.NewProvidedSequentialExpr(providedExpr1, providedExpr2);
      }

      else if (data.ProvidedTryWithExpr != null)
      {
        var providedTryWithExpr = data.ProvidedTryWithExpr;
        var providedExpr1 = Create(providedTryWithExpr.ProvidedExpr1, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var providedVar1 =
          ProxyProvidedVarWithCache.Create(providedTryWithExpr.ProvidedVar1, myTypeProviderId, myProcessModel,
            myContext, myCache);
        var providedExpr2 = Create(providedTryWithExpr.ProvidedExpr2, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var providedVar2 =
          ProxyProvidedVarWithCache.Create(providedTryWithExpr.ProvidedVar2, myTypeProviderId, myProcessModel,
            myContext, myCache);
        var providedExpr3 = Create(providedTryWithExpr.ProvidedExpr3, myTypeProviderId, myProcessModel, myContext,
          myCache);
        return ProvidedExprType.NewProvidedTryWithExpr(providedExpr1, providedVar1, providedExpr2, providedVar2,
          providedExpr3);
      }

      else if (data.ProvidedTryFinallyExpr != null)
      {
        var providedTryFinallyExpr = data.ProvidedTryFinallyExpr;
        var providedExpr1 = Create(providedTryFinallyExpr.ProvidedExpr1, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var providedExpr2 = Create(providedTryFinallyExpr.ProvidedExpr2, myTypeProviderId, myProcessModel, myContext,
          myCache);
        return ProvidedExprType.NewProvidedTryFinallyExpr(providedExpr1, providedExpr2);
      }

      else if (data.ProvidedLambdaExpr != null)
      {
        var providedLambdaExpr = data.ProvidedLambdaExpr;
        var providedVar =
          ProxyProvidedVarWithCache.Create(providedLambdaExpr.ProvidedVar, myTypeProviderId, myProcessModel, myContext,
            myCache);
        var providedExpr = Create(providedLambdaExpr.ProvidedExpr, myTypeProviderId, myProcessModel, myContext,
          myCache);
        return ProvidedExprType.NewProvidedLambdaExpr(providedVar, providedExpr);
      }

      else if (data.ProvidedCallExpr != null)
      {
        var providedCallExpr = data.ProvidedCallExpr;
        var providedExpr = providedCallExpr.ProvidedExpr == null
          ? FSharpOption<ProvidedExpr>.None
          : Create(providedCallExpr.ProvidedExpr, myTypeProviderId, myProcessModel, myContext, myCache);
        var providedMethodInfo = ProxyProvidedMethodInfoWithCache.Create(providedCallExpr.ProvidedMethodInfo, myTypeProviderId,
          myProcessModel, myContext, myCache);
        var providedExprs = providedCallExpr.ProvidedExprs
          .Select(t => Create(t, myTypeProviderId, myProcessModel, myContext, myCache))
          .ToArray();
        // ReSharper disable once CoVariantArrayConversion
        return ProvidedExprType.NewProvidedCallExpr(providedExpr, providedMethodInfo, providedExprs);
      }

      else if (data.ProvidedConstantExpr != null)
      {
        var providedConstantExpr = data.ProvidedConstantExpr;
        var providedType =
          myCache.GetOrCreateWithContext(providedConstantExpr.ProvidedType, myTypeProviderId, myContext);
        var obj = providedConstantExpr.Obj.Unbox();
        return ProvidedExprType.NewProvidedConstantExpr(obj, providedType);
      }

      else if (data.ProvidedDefaultExpr != null)
      {
        var providedDefaultExpr = data.ProvidedDefaultExpr;
        var providedType =
          myCache.GetOrCreateWithContext(providedDefaultExpr.ProvidedType, myTypeProviderId, myContext);
        return ProvidedExprType.NewProvidedDefaultExpr(providedType);
      }

      else if (data.ProvidedNewTupleExpr != null)
      {
        var providedNewTupleExpr = data.ProvidedNewTupleExpr;
        var providedExprs = providedNewTupleExpr.ProvidedExprs
          .Select(t => Create(t, myTypeProviderId, myProcessModel, myContext, myCache))
          .ToArray();
        // ReSharper disable once CoVariantArrayConversion
        return ProvidedExprType.NewProvidedNewTupleExpr(providedExprs);
      }

      else if (data.ProvidedTupleGetExpr != null)
      {
        var providedTupleGetExpr = data.ProvidedTupleGetExpr;
        var providedExpr = Create(providedTupleGetExpr.ProvidedExpr, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var intValue = providedTupleGetExpr.Int;
        return ProvidedExprType.NewProvidedTupleGetExpr(providedExpr, intValue);
      }

      else if (data.ProvidedTypeAsExpr != null)
      {
        var providedTypeAsExpr = data.ProvidedTypeAsExpr;
        var providedExpr = Create(providedTypeAsExpr.ProvidedExpr, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var providedType = myCache.GetOrCreateWithContext(providedTypeAsExpr.ProvidedType, myTypeProviderId, myContext);
        return ProvidedExprType.NewProvidedTypeAsExpr(providedExpr, providedType);
      }

      else if (data.ProvidedTypeTestExpr != null)
      {
        var providedTypeTestExpr = data.ProvidedTypeTestExpr;
        var providedExpr = Create(providedTypeTestExpr.ProvidedExpr, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var providedType =
          myCache.GetOrCreateWithContext(providedTypeTestExpr.ProvidedType, myTypeProviderId, myContext);
        return ProvidedExprType.NewProvidedTypeTestExpr(providedExpr, providedType);
      }

      else if (data.ProvidedLetExpr != null)
      {
        var providedLetExpr = data.ProvidedLetExpr;
        var providedVar =
          ProxyProvidedVarWithCache.Create(providedLetExpr.ProvidedVar, myTypeProviderId, myProcessModel, myContext,
            myCache);
        var providedExpr1 = Create(providedLetExpr.ProvidedExpr1, myTypeProviderId, myProcessModel, myContext, myCache);
        var providedExpr2 = Create(providedLetExpr.ProvidedExpr2, myTypeProviderId, myProcessModel, myContext, myCache);
        return ProvidedExprType.NewProvidedLetExpr(providedVar, providedExpr1, providedExpr2);
      }

      else if (data.ProvidedVarSetExpr != null)
      {
        var providedVarSetExpr = data.ProvidedVarSetExpr;
        var providedVar =
          ProxyProvidedVarWithCache.Create(providedVarSetExpr.ProvidedVar, myTypeProviderId, myProcessModel, myContext,
            myCache);
        var providedExpr = Create(providedVarSetExpr.ProvidedExpr, myTypeProviderId, myProcessModel, myContext,
          myCache);
        return ProvidedExprType.NewProvidedVarSetExpr(providedVar, providedExpr);
      }

      else if (data.ProvidedIfThenElseExpr != null)
      {
        var providedIfThenElseExpr = data.ProvidedIfThenElseExpr;
        var providedExpr1 = Create(providedIfThenElseExpr.ProvidedExpr1, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var providedExpr2 = Create(providedIfThenElseExpr.ProvidedExpr2, myTypeProviderId, myProcessModel, myContext,
          myCache);
        var providedExpr3 = Create(providedIfThenElseExpr.ProvidedExpr3, myTypeProviderId, myProcessModel, myContext,
          myCache);
        return ProvidedExprType.NewProvidedIfThenElseExpr(providedExpr1, providedExpr2, providedExpr3);
      }

      else if (data.ProvidedVarExpr != null)
      {
        var providedVarExpr = data.ProvidedVarExpr;
        var providedVar =
          ProxyProvidedVarWithCache.Create(providedVarExpr.ProvidedVar, myTypeProviderId, myProcessModel, myContext,
            myCache);
        return ProvidedExprType.NewProvidedVarExpr(providedVar);
      }

      throw new InvalidOperationException("ProvidedExprType has flag HasValue, but data is null");
    }

    private int? myTypeId;
    private readonly InterruptibleLazy<FSharpOption<ProvidedExprType>> myExprType;
  }
}
