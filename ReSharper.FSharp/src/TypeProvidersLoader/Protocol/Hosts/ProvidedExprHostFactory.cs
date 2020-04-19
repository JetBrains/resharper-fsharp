using System;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public class ProvidedExprHostFactory : IOutOfProcessHostFactory<RdProvidedExprProcessModel>
  {
    private readonly IReadProvidedCache<Tuple<ProvidedExpr, int>> myProvidedExprsCache;
    private readonly IProvidedRdModelsCreator<ProvidedType, RdProvidedType> myProvidedTypeRdModelsCreator;
    private readonly IProvidedRdModelsCreator<ProvidedExpr, RdProvidedExpr> myProvidedExprRdModelsCreator;
    private readonly IProvidedRdModelsCreator<ProvidedVar, RdProvidedVar> myProvidedVarRdModelsCreator;
    private readonly IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> myProvidedMethodRdModelsCreator;

    private readonly IProvidedRdModelsCreator<ProvidedConstructorInfo, RdProvidedConstructorInfo>
      myProvidedConstructorRdModelsCreator;

    public ProvidedExprHostFactory(IReadProvidedCache<Tuple<ProvidedExpr, int>> providedExprsCache,
      IProvidedRdModelsCreator<ProvidedType, RdProvidedType> providedTypeRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedExpr, RdProvidedExpr> providedExprRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedVar, RdProvidedVar> providedVarRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedMethodInfo, RdProvidedMethodInfo> providedMethodRdModelsCreator,
      IProvidedRdModelsCreator<ProvidedConstructorInfo, RdProvidedConstructorInfo> providedConstructorRdModelsCreator)
    {
      myProvidedExprsCache = providedExprsCache;
      myProvidedTypeRdModelsCreator = providedTypeRdModelsCreator;
      myProvidedExprRdModelsCreator = providedExprRdModelsCreator;
      myProvidedVarRdModelsCreator = providedVarRdModelsCreator;
      myProvidedMethodRdModelsCreator = providedMethodRdModelsCreator;
      myProvidedConstructorRdModelsCreator = providedConstructorRdModelsCreator;
    }

    public void Initialize(RdProvidedExprProcessModel model)
    {
      model.Type.Set(GetType);
      model.GetExprType.Set(GetExprType);
    }

    private RdTask<RdProvidedExprType> GetExprType(Lifetime lifetime, int entityId)
    {
      var (providedExpr, typeProviderId) = myProvidedExprsCache.Get(entityId);
      var exprType = providedExpr.GetExprType();

      if (FSharpOption<ProvidedExprType>.get_IsNone(exprType))
      {
        return RdTask<RdProvidedExprType>.Successful(new RdProvidedExprType(false, null, null, null, null, null, null,
          null, null, null, null, null, null, null, null, null, null, null, null, null, null));
      }

      var exprTypeValue = exprType.Value;

      ProvidedNewArrayExpr providedNewArrayExpr = null;
      ProvidedNewObjectExpr providedNewObjectExpr = null;
      ProvidedWhileLoopExpr providedWhileLoopExpr = null;
      ProvidedNewDelegateExpr providedNewDelegateExpr = null;
      ProvidedForIntegerRangeLoopExpr providedForIntegerRangeLoopExpr = null;
      ProvidedSequentialExpr providedSequentialExpr = null;
      ProvidedTryWithExpr providedTryWithExpr = null;
      ProvidedTryFinallyExpr providedTryFinallyExpr = null;
      ProvidedLambdaExpr providedLambdaExpr = null;
      ProvidedCallExpr providedCallExpr = null;
      ProvidedConstantExpr providedConstantExpr = null;
      ProvidedDefaultExpr providedDefaultExpr = null;
      ProvidedNewTupleExpr providedNewTupleExpr = null;
      ProvidedTupleGetExpr providedTupleGetExpr = null;
      ProvidedTypeAsExpr providedTypeAsExpr = null;
      ProvidedTypeTestExpr providedTypeTestExpr = null;
      ProvidedLetExpr providedLetExpr = null;
      ProvidedVarSetExpr providedVarSetExpr = null;
      ProvidedIfThenElseExpr providedIfThenElseExpr = null;
      ProvidedVarExpr providedVarExpr = null;

      if (exprTypeValue is ProvidedExprType.ProvidedNewArrayExpr providedNewArrayExprType)
      {
        var providedTypeId = myProvidedTypeRdModelsCreator.CreateRdModel(providedNewArrayExprType.Item1, typeProviderId)
          .EntityId;
        var providedExprs = providedNewArrayExprType.Item2
          .Select(t => myProvidedExprRdModelsCreator.CreateRdModel(t, typeProviderId))
          .ToArray();
        providedNewArrayExpr = new ProvidedNewArrayExpr(providedTypeId, providedExprs);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedNewObjectExpr providedNewObjectExprType)
      {
        var providedConstructorInfo =
          myProvidedConstructorRdModelsCreator.CreateRdModel(providedNewObjectExprType.Item1, typeProviderId);
        var providedExprs = providedNewObjectExprType.Item2
          .Select(t => myProvidedExprRdModelsCreator.CreateRdModel(t, typeProviderId))
          .ToArray();
        providedNewObjectExpr = new ProvidedNewObjectExpr(providedConstructorInfo, providedExprs);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedWhileLoopExpr providedWhileLoopExprType)
      {
        var providedExpr1 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedWhileLoopExprType.Item1, typeProviderId);
        var providedExpr2 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedWhileLoopExprType.Item2, typeProviderId);
        providedWhileLoopExpr = new ProvidedWhileLoopExpr(providedExpr1, providedExpr2);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedNewDelegateExpr providedNewDelegateExprType)
      {
        var providedTypeId = myProvidedTypeRdModelsCreator
          .CreateRdModel(providedNewDelegateExprType.Item1, typeProviderId).EntityId;
        var providedVars = providedNewDelegateExprType.Item2
          .Select(t => myProvidedVarRdModelsCreator.CreateRdModel(t, typeProviderId))
          .ToArray();
        var providedExprModel =
          myProvidedExprRdModelsCreator.CreateRdModel(providedNewDelegateExprType.Item3, typeProviderId);
        providedNewDelegateExpr = new ProvidedNewDelegateExpr(providedTypeId, providedVars, providedExprModel);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedForIntegerRangeLoopExpr providedForIntegerRangeLoopExprType)
      {
        var providedVar =
          myProvidedVarRdModelsCreator.CreateRdModel(providedForIntegerRangeLoopExprType.Item1, typeProviderId);
        var providedExpr1 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedForIntegerRangeLoopExprType.Item2, typeProviderId);
        var providedExpr2 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedForIntegerRangeLoopExprType.Item3, typeProviderId);
        var providedExpr3 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedForIntegerRangeLoopExprType.Item4, typeProviderId);
        providedForIntegerRangeLoopExpr =
          new ProvidedForIntegerRangeLoopExpr(providedVar, providedExpr1, providedExpr2, providedExpr3);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedSequentialExpr providedSequentialExprType)
      {
        var providedExpr1 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedSequentialExprType.Item1, typeProviderId);
        var providedExpr2 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedSequentialExprType.Item2, typeProviderId);
        providedSequentialExpr = new ProvidedSequentialExpr(providedExpr1, providedExpr2);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedTryWithExpr providedTryWithExprType)
      {
        var providedExpr1 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedTryWithExprType.Item1, typeProviderId);
        var providedVar1 =
          myProvidedVarRdModelsCreator.CreateRdModel(providedTryWithExprType.Item2, typeProviderId);
        var providedExpr2 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedTryWithExprType.Item3, typeProviderId);
        var providedVar2 =
          myProvidedVarRdModelsCreator.CreateRdModel(providedTryWithExprType.Item4, typeProviderId);
        var providedExpr3 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedTryWithExprType.Item5, typeProviderId);
        providedTryWithExpr =
          new ProvidedTryWithExpr(providedExpr1, providedVar1, providedExpr2, providedVar2, providedExpr3);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedTryFinallyExpr providedTryFinallyExprType)
      {
        var providedExpr1 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedTryFinallyExprType.Item1, typeProviderId);
        var providedExpr2 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedTryFinallyExprType.Item2, typeProviderId);
        providedTryFinallyExpr = new ProvidedTryFinallyExpr(providedExpr1, providedExpr2);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedLambdaExpr providedLambdaExprType)
      {
        var providedVar =
          myProvidedVarRdModelsCreator.CreateRdModel(providedLambdaExprType.Item1, typeProviderId);
        var providedExprModel =
          myProvidedExprRdModelsCreator.CreateRdModel(providedLambdaExprType.Item2, typeProviderId);
        providedLambdaExpr = new ProvidedLambdaExpr(providedVar, providedExprModel);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedCallExpr providedCallExprType)
      {
        var providedExprModel = FSharpOption<ProvidedExpr>.get_IsSome(providedCallExprType.Item1)
          ? myProvidedExprRdModelsCreator.CreateRdModel(providedCallExprType.Item1.Value, typeProviderId)
          : null;
        var providedMethod = myProvidedMethodRdModelsCreator.CreateRdModel(providedCallExprType.Item2, typeProviderId);
        var providedExprs = providedCallExprType.Item3
          .Select(t => myProvidedExprRdModelsCreator.CreateRdModel(t, typeProviderId))
          .ToArray();
        providedCallExpr = new ProvidedCallExpr(providedExprModel, providedMethod, providedExprs);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedConstantExpr providedConstantExprType)
      {
        var obj = providedConstantExprType.Item1.BoxToClientStaticArg();
        var providedTypeId = myProvidedTypeRdModelsCreator.CreateRdModel(providedConstantExprType.Item2, typeProviderId)
          .EntityId;
        providedConstantExpr = new ProvidedConstantExpr(obj, providedTypeId);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedDefaultExpr providedDefaultExprType)
      {
        var providedTypeId = myProvidedTypeRdModelsCreator.CreateRdModel(providedDefaultExprType.Item, typeProviderId)
          .EntityId;
        providedDefaultExpr = new ProvidedDefaultExpr(providedTypeId);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedNewTupleExpr providedNewTupleExprType)
      {
        var providedExprs = providedNewTupleExprType.Item
          .Select(t => myProvidedExprRdModelsCreator.CreateRdModel(t, typeProviderId))
          .ToArray();
        providedNewTupleExpr = new ProvidedNewTupleExpr(providedExprs);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedTupleGetExpr providedTupleGetExprType)
      {
        var providedExprModel =
          myProvidedExprRdModelsCreator.CreateRdModel(providedTupleGetExprType.Item1, typeProviderId);
        var intValue = providedTupleGetExprType.Item2;
        providedTupleGetExpr = new ProvidedTupleGetExpr(providedExprModel, intValue);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedTypeAsExpr providedTypeAsExprType)
      {
        var providedExprModel =
          myProvidedExprRdModelsCreator.CreateRdModel(providedTypeAsExprType.Item1, typeProviderId);
        var providedTypeId = myProvidedTypeRdModelsCreator.CreateRdModel(providedTypeAsExprType.Item2, typeProviderId)
          .EntityId;
        providedTypeAsExpr = new ProvidedTypeAsExpr(providedExprModel, providedTypeId);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedTypeTestExpr providedTypeTestExprType)
      {
        var providedExprModel =
          myProvidedExprRdModelsCreator.CreateRdModel(providedTypeTestExprType.Item1, typeProviderId);
        var providedTypeId = myProvidedTypeRdModelsCreator.CreateRdModel(providedTypeTestExprType.Item2, typeProviderId)
          .EntityId;
        providedTypeTestExpr = new ProvidedTypeTestExpr(providedExprModel, providedTypeId);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedLetExpr providedLetExprType)
      {
        var providedVar =
          myProvidedVarRdModelsCreator.CreateRdModel(providedLetExprType.Item1, typeProviderId);
        var providedExpr1 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedLetExprType.Item2, typeProviderId);
        var providedExpr2 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedLetExprType.Item3, typeProviderId);
        providedLetExpr = new ProvidedLetExpr(providedVar, providedExpr1, providedExpr2);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedVarSetExpr providedVarSetExprType)
      {
        var providedVar =
          myProvidedVarRdModelsCreator.CreateRdModel(providedVarSetExprType.Item1, typeProviderId);
        var providedExprModel =
          myProvidedExprRdModelsCreator.CreateRdModel(providedVarSetExprType.Item2, typeProviderId);
        providedVarSetExpr = new ProvidedVarSetExpr(providedVar, providedExprModel);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedIfThenElseExpr providedIfThenElseExprType)
      {
        var providedExpr1 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedIfThenElseExprType.Item1, typeProviderId);
        var providedExpr2 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedIfThenElseExprType.Item2, typeProviderId);
        var providedExpr3 =
          myProvidedExprRdModelsCreator.CreateRdModel(providedIfThenElseExprType.Item3, typeProviderId);
        providedIfThenElseExpr = new ProvidedIfThenElseExpr(providedExpr1, providedExpr2, providedExpr3);
      }

      else if (exprTypeValue is ProvidedExprType.ProvidedVarExpr providedVarExprType)
      {
        var providedVar =
          myProvidedVarRdModelsCreator.CreateRdModel(providedVarExprType.Item, typeProviderId);
        providedVarExpr = new ProvidedVarExpr(providedVar);
      }

      return RdTask<RdProvidedExprType>.Successful(new RdProvidedExprType(true, providedNewArrayExpr,
        providedNewObjectExpr, providedWhileLoopExpr, providedNewDelegateExpr, providedForIntegerRangeLoopExpr,
        providedSequentialExpr, providedTryWithExpr, providedTryFinallyExpr, providedLambdaExpr, providedCallExpr,
        providedConstantExpr, providedDefaultExpr, providedNewTupleExpr, providedTupleGetExpr, providedTypeAsExpr,
        providedTypeTestExpr, providedLetExpr, providedVarSetExpr, providedIfThenElseExpr, providedVarExpr));
    }

    private RdTask<int> GetType(Lifetime lifetime, int entityId)
    {
      var (providedExpr, typeProviderId) = myProvidedExprsCache.Get(entityId);
      var typeId = myProvidedTypeRdModelsCreator.CreateRdModel(providedExpr.Type, typeProviderId).EntityId;
      return RdTask<int>.Successful(typeId);
    }
  }
}
