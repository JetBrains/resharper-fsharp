namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open System
open System.Collections.Generic
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Refactorings.Inline
open JetBrains.ReSharper.Refactorings.InlineVar
open JetBrains.ReSharper.Refactorings.InlineVar.RDAnalysis
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type FSharpInlineHelper(driver) =
    inherit InlineHelper(driver)

    override x.GetContainingStatement(treeNode) = treeNode

    override x.ReplaceTypeParameter(_, _, _) = raise (NotImplementedException())
    override x.GetParameter2Argument(_, _) = raise (NotImplementedException())
    override x.GetArgument2Infos(_, _, _) = raise (NotImplementedException())
    override x.CanHaveSideEffects(_: IArgument): bool = raise (NotImplementedException())
    override x.CanHaveSideEffects(_: IExpression): bool = raise (NotImplementedException())
    override x.GetQualifierExpression _ = raise (NotImplementedException())
    override x.InsertReturnValueTempVariable(_, _, _, _) = raise (NotImplementedException())
    override x.InsertTempForQualifier(_, _, _, _, _) = raise (NotImplementedException())
    override x.InsertTempVariableForAssignedValue(_, _, _, _) = raise (NotImplementedException())
    override x.InsertTempForArgument(_, _, _, _, _, _) = raise (NotImplementedException())
    override x.RemoveCastFromElement _ = raise (NotImplementedException())
    override x.GetArgumentOwner(_, _) = raise (NotImplementedException())
    override x.AllNotQualifiableReferences _ = raise (NotImplementedException())
    override x.SplitDeclarationAndInitializer _ = raise (NotImplementedException())

type FSharpInlineVariable(workflow, solution, driver) =
    inherit InlineVarBase(workflow, solution, driver)

    override x.InlineHelper = FSharpInlineHelper(driver) :> _

    override x.ProcessReferenceWithContext(reference, _, info) =
        let referenceOwner = reference.GetTreeNode()
        use cookie = WriteLockCookie.Create(referenceOwner.IsPhysical())

        let expr = info.InlinedMethodInfo.Expression :?> IFSharpExpression

        let oldNode, newNode =
            match referenceOwner with
            | :? IExpressionReferenceName as referenceName when
                    isNotNull (ReferenceNameOwnerPatNavigator.GetByReferenceName(referenceName)) ->
                let factory = referenceOwner.CreateElementFactory()
                let newPattern = factory.CreatePattern(expr.GetText(), false) :> ITreeNode
                let oldPattern = ReferenceNameOwnerPatNavigator.GetByReferenceName(referenceName) :> ITreeNode
                oldPattern, newPattern
            | _ -> referenceOwner, expr.Copy()

        let newNode = ModificationUtil.ReplaceChild(oldNode, newNode)
        let newExpr = newNode.As<IFSharpExpression>()
        if isNotNull newExpr then
            addParensIfNeeded newExpr |> ignore

    override x.Ignore _ = false

    override x.RemoveVariableDeclaration(decl) =
        use cookie = WriteLockCookie.Create(decl.IsPhysical())

        let refPat = decl.As<IReferencePat>()
        let binding = BindingNavigator.GetByHeadPattern(refPat.IgnoreParentParens())
        match LetBindingsNavigator.GetByBinding(binding) with
        | :? ILetOrUseExpr as letExpr ->
            replaceWithCopy letExpr letExpr.InExpression

        | :? ILetBindingsDeclaration as letDecl ->
            removeModuleMember letDecl

        | _ -> ()

    override x.RemoveAssignment(expr) = ()


type FSharpInlineVarAnalyser(workflow) =
    inherit InlineVarAnalyserBase(workflow)

    let [<Literal>] cannotInline = "Cannot inline value."

    let mutable inlineExpr = null
    let mutable inlineReferences = null

    let canTransformToPattern (expr: IFSharpExpression) =
        match expr.IgnoreInnerParens() with
        | :? IConstExpr -> true
        | :? IReferenceExpr as refExpr ->
            match refExpr.Reference.Resolve().DeclaredElement with
            | :? IField as field -> field.IsEnumMember || field.IsConstant && field.IsStatic
            | _ -> false
        | _ -> false

    override val InlineAll = true with get, set

    override x.References = inlineReferences
    override x.Expression = inlineExpr
    override x.AssignmentExpression = inlineExpr

    override x.Run(declaredElement, _, references) =
        let refPat = declaredElement.GetDeclarations().SingleItem().As<IReferencePat>()
        if isNull refPat then Pair(false, cannotInline) else

        let isTopLevel = declaredElement :? ITopLevelPatternDeclaredElement

        let binding = BindingNavigator.GetByHeadPattern(refPat.IgnoreParentParens())
        if isNull binding || isNull binding.Expression then Pair(false, cannotInline) else

        if binding.ParametersDeclarationsEnumerable.Any() then Pair(false, "Cannot inline function.") else

        let letBindings = LetBindingsNavigator.GetByBinding(binding)
        if isNull letBindings || letBindings.Bindings.Count <> 1 then Pair(false, cannotInline) else

        let letExpr = letBindings.As<ILetOrUseExpr>()
        if isNotNull letExpr && isNull letExpr.InExpression then Pair(false, cannotInline) else

        references
        |> Seq.tryPick (fun reference ->
            let treeNode = reference.GetTreeNode()
            if isTopLevel && treeNode.GetContainingFile() != refPat.GetContainingFile() then
                Some "Value has non-local usages." else

            match treeNode with
            | :? IReferenceExpr as refExpr ->
                let expr = refExpr.IgnoreParentParens()
                if isNull expr then None else

                let setExpr = SetExprNavigator.GetByLeftExpression(expr)
                let indexerExpr = IndexerExprNavigator.GetByQualifier(expr).IgnoreParentParens()
                let indexerSetExpr = SetExprNavigator.GetByLeftExpression(indexerExpr).IgnoreParentParens()
                if isNotNull setExpr || isNotNull indexerSetExpr then
                    Some "Value has write usages." else

                if isNotNull (AddressOfExprNavigator.GetByExpression(expr)) then
                    Some "Value has 'address of' usages." else

                None

            | :? IReferencePat when not (canTransformToPattern binding.Expression) ->
                Some "Can't inline expression to a pattern"

            | _ -> None
        )
        |> Option.map (fun error -> Pair(false, error))
        |> Option.defaultWith (fun _ ->
            inlineExpr <- binding.Expression
            inlineReferences <- List(references)

            Pair(true, ""))
