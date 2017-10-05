namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages
//
//open System
//open System.Collections.Generic
//open System.Linq
//open JetBrains.Annotations
//open JetBrains.DocumentModel
//open JetBrains.ReSharper.Daemon.UsageChecking
//open JetBrains.ReSharper.Feature.Services.Daemon
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
//open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
//open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
//open JetBrains.ReSharper.Psi.Tree
//open JetBrains.Util.Extension
//open JetBrains.ReSharper.Daemon.VisualElements
//open Microsoft.FSharp.Compiler.SourceCodeServices
//open JetBrains.ReSharper.Psi.Colors
//open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings
//open JetBrains.ReSharper.Feature.Services.Daemon.IdeaAttributes
//
//[<DaemonStage(StagesBefore = [| typeof<HighlightOpenExpressionsStage> |], StagesAfter = [| typeof<CollectUsagesStage> |])>]
//type ColorsHighlightStage() =
//    inherit FSharpDaemonStageBase()
//
//    override x.CreateProcess(fsFile, daemonProcess) =
//        ColorsHighlightStageProcess(fsFile, daemonProcess) :> _
//
//and ColorsHighlightStageProcess(fsFile: IFSharpFile, daemonProcess) =
//    inherit FSharpDaemonStageProcessBase(daemonProcess) // todo: completion: no items from primary ctor (FCS issue)
//    let highlightings = List<HighlightingInfo>()
//    let document = fsFile.GetSourceFile().Document
//
//    let rec visitExpr f (e:FSharpExpr) = 
//        f e
//        match e with 
//        | BasicPatterns.AddressOf(lvalueExpr) -> 
//            visitExpr f lvalueExpr
//        | BasicPatterns.AddressSet(lvalueExpr, rvalueExpr) -> 
//            visitExpr f lvalueExpr; visitExpr f rvalueExpr
//        | BasicPatterns.Application(funcExpr, typeArgs, argExprs) -> 
//            visitExpr f funcExpr; visitExprs f argExprs
//        | BasicPatterns.Call(objExprOpt, memberOrFunc, typeArgs1, typeArgs2, argExprs) ->
////            if memberOrFunc.CompiledName.Equals("FromArgb", StringComparison.Ordinal) then
////                match memberOrFunc.EnclosingEntity with
////                | Some entity when entity.QualifiedName.SubstringBefore(",", StringComparison.Ordinal).Equals("System.Drawing.Color") ->
//            let range = DocumentRange(document, 100)
//            highlightings.Add(HighlightingInfo(range, FSharpIdentifierHighlighting(IdeaHighlightingAttributeIds.INLINE_PARAMETER_HINT, range)))
//            visitObjArg f objExprOpt; visitExprs f argExprs
//        | BasicPatterns.Coerce(targetType, inpExpr) -> 
//            visitExpr f inpExpr
//        | BasicPatterns.FastIntegerForLoop(startExpr, limitExpr, consumeExpr, isUp) -> 
//            visitExpr f startExpr; visitExpr f limitExpr; visitExpr f consumeExpr
//        | BasicPatterns.ILAsm(asmCode, typeArgs, argExprs) -> 
//            visitExprs f argExprs
//        | BasicPatterns.ILFieldGet (objExprOpt, fieldType, fieldName) -> 
//            visitObjArg f objExprOpt
//        | BasicPatterns.ILFieldSet (objExprOpt, fieldType, fieldName, valueExpr) -> 
//            visitObjArg f objExprOpt
//        | BasicPatterns.IfThenElse (guardExpr, thenExpr, elseExpr) -> 
//            visitExpr f guardExpr; visitExpr f thenExpr; visitExpr f elseExpr
//        | BasicPatterns.Lambda(lambdaVar, bodyExpr) -> 
//            visitExpr f bodyExpr
//        | BasicPatterns.Let((bindingVar, bindingExpr), bodyExpr) -> 
//            visitExpr f bindingExpr; visitExpr f bodyExpr
//        | BasicPatterns.LetRec(recursiveBindings, bodyExpr) -> 
//            List.iter (snd >> visitExpr f) recursiveBindings; visitExpr f bodyExpr
//        | BasicPatterns.NewArray(arrayType, argExprs) -> 
//            visitExprs f argExprs
//        | BasicPatterns.NewDelegate(delegateType, delegateBodyExpr) -> 
//            visitExpr f delegateBodyExpr
//        | BasicPatterns.NewObject(objType, typeArgs, argExprs) ->
//            visitExprs f argExprs
//        | BasicPatterns.NewRecord(recordType, argExprs) ->  
//            visitExprs f argExprs
//        | BasicPatterns.NewTuple(tupleType, argExprs) -> 
//            visitExprs f argExprs
//        | BasicPatterns.NewUnionCase(unionType, unionCase, argExprs) -> 
//            visitExprs f argExprs
//        | BasicPatterns.Quote(quotedExpr) -> 
//            visitExpr f quotedExpr
//        | BasicPatterns.FSharpFieldGet(objExprOpt, recordOrClassType, fieldInfo) -> 
//            visitObjArg f objExprOpt
//        | BasicPatterns.FSharpFieldSet(objExprOpt, recordOrClassType, fieldInfo, argExpr) -> 
//            visitObjArg f objExprOpt; visitExpr f argExpr
//        | BasicPatterns.Sequential(firstExpr, secondExpr) -> 
//            visitExpr f firstExpr; visitExpr f secondExpr
//        | BasicPatterns.TryFinally(bodyExpr, finalizeExpr) -> 
//            visitExpr f bodyExpr; visitExpr f finalizeExpr
//        | BasicPatterns.TryWith(bodyExpr, _, _, catchVar, catchExpr) -> 
//            visitExpr f bodyExpr; visitExpr f catchExpr
//        | BasicPatterns.TupleGet(tupleType, tupleElemIndex, tupleExpr) -> 
//            visitExpr f tupleExpr
//        | BasicPatterns.DecisionTree(decisionExpr, decisionTargets) -> 
//            visitExpr f decisionExpr; List.iter (snd >> visitExpr f) decisionTargets
//        | BasicPatterns.DecisionTreeSuccess (decisionTargetIdx, decisionTargetExprs) -> 
//            visitExprs f decisionTargetExprs
//        | BasicPatterns.TypeLambda(genericParam, bodyExpr) -> 
//            visitExpr f bodyExpr
//        | BasicPatterns.TypeTest(ty, inpExpr) -> 
//            visitExpr f inpExpr
//        | BasicPatterns.UnionCaseSet(unionExpr, unionType, unionCase, unionCaseField, valueExpr) -> 
//            visitExpr f unionExpr; visitExpr f valueExpr
//        | BasicPatterns.UnionCaseGet(unionExpr, unionType, unionCase, unionCaseField) -> 
//            visitExpr f unionExpr
//        | BasicPatterns.UnionCaseTest(unionExpr, unionType, unionCase) -> 
//            visitExpr f unionExpr
//        | BasicPatterns.UnionCaseTag(unionExpr, unionType) -> 
//            visitExpr f unionExpr
//        | BasicPatterns.ObjectExpr(objType, baseCallExpr, overrides, interfaceImplementations) -> 
//            visitExpr f baseCallExpr
//            List.iter (visitObjMember f) overrides
//            List.iter (snd >> List.iter (visitObjMember f)) interfaceImplementations
//        | BasicPatterns.TraitCall(sourceTypes, traitName, typeArgs, typeInstantiation, argTypes, argExprs) -> 
//            visitExprs f argExprs
//        | BasicPatterns.ValueSet(valToSet, valueExpr) -> 
//            visitExpr f valueExpr
//        | BasicPatterns.WhileLoop(guardExpr, bodyExpr) -> 
//            visitExpr f guardExpr; visitExpr f bodyExpr
//        | BasicPatterns.BaseValue baseType -> ()
//        | BasicPatterns.DefaultValue defaultType -> ()
//        | BasicPatterns.ThisValue thisType -> ()
//        | BasicPatterns.Const(constValueObj, constType) -> ()
//        | BasicPatterns.Value(valueToGet) -> ()
//        | _ -> ()
//
//    and visitExprs f exprs = 
//        List.iter (visitExpr f) exprs
//    
//    and visitObjArg f objOpt = 
//        Option.iter (visitExpr f) objOpt
//    
//    and visitObjMember f memb = 
//        visitExpr f memb.Body
//
//    let rec processEntities (decls: FSharpImplementationFileDeclaration list) =
//        decls
//        |> List.iter (fun decl ->
//            match decl with
//            | Entity (_, entities) -> processEntities entities
//            | MemberOrFunctionOrValue (_, _, expr) -> visitExpr (fun _ -> ()) expr
//            | InitAction (expr) -> visitExpr (fun _ -> ()) expr
//        )
//
//    override x.Execute(committer) =
//        match fsFile.GetParseAndCheckResults() with
//        | Some parseAndCheckResults ->
////            committer.Invoke(DaemonStageResult(highlightings))
//            match parseAndCheckResults.CheckResults.ImplementationFiles with
//            | Some files -> files |> List.iter (fun f -> processEntities f.Declarations)
//            | _ -> ()
//        | _ -> ()
//        committer.Invoke(DaemonStageResult(highlightings))