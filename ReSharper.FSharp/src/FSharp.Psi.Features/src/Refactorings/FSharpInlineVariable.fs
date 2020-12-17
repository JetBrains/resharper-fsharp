namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open System
open System.Collections.Generic
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Resolve
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

type FSharpInlineVariable(workflow, solution, driver) =
    inherit InlineVarBase(workflow, solution, driver)

    let mutable exprIndent = 0

    override x.InlineHelper = FSharpInlineHelper(driver) :> _

    override x.ProcessReferenceWithContext(reference, _, info) =
        let referenceOwner = reference.GetTreeNode()
        use cookie = WriteLockCookie.Create(referenceOwner.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let expr = info.InlinedMethodInfo.Expression :?> IFSharpExpression
        let exprCopy = expr.Copy()

        let indentShift = referenceOwner.Indent - exprIndent
        shiftExpr indentShift exprCopy
        
        let newExpr = ModificationUtil.ReplaceChild(referenceOwner, exprCopy)
        addParensIfNeeded newExpr |> ignore
 
    override x.Ignore _ = false

    override x.RemoveVariableDeclaration(decl) =
        let refPat = decl.As<ILocalReferencePat>()
        let binding = BindingNavigator.GetByHeadPattern(refPat.IgnoreParentParens())
        let letExpr = LetOrUseExprNavigator.GetByBinding(binding)

        let inKeyword = letExpr.InKeyword
        let lastNode: ITreeNode = if isNotNull inKeyword then inKeyword :> _ else binding :> _

        let first =
            skipMatchingNodesAfter isInlineSpaceOrComment lastNode
            |> getThisOrNextNewLine
            |> skipMatchingNodesAfter isInlineSpace

        let last = letExpr.LastChild

        use cookie = WriteLockCookie.Create(letExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        ModificationUtil.AddChildRangeBefore(letExpr, TreeRange(first, last)) |> ignore
        ModificationUtil.DeleteChild(letExpr)

    override x.RemoveAssignment(expr) =
        exprIndent <- expr.Indent


type FSharpInlineVarAnalyser(workflow) =
    inherit InlineVarAnalyserBase(workflow)

    let mutable inlineExpr = null
    let mutable inlineReferences = null

    override val InlineAll = true with get, set

    override x.References = inlineReferences
    override x.Expression = inlineExpr
    override x.AssignmentExpression = inlineExpr

    override x.Run(declaredElement, _, references) =
        let refPat = declaredElement.As<ILocalReferencePat>()
        if isNull refPat then Pair(false, "") else

        let binding = BindingNavigator.GetByHeadPattern(refPat.IgnoreParentParens())
        if isNull binding || isNull binding.Expression then Pair(false, "") else

        let letExpr = LetOrUseExprNavigator.GetByBinding(binding)
        if isNull letExpr || letExpr.Bindings.Count <> 1 || isNull letExpr.InExpression then Pair(false, "") else

        let hasWriteReferences (references: IList<IReference>) =
            references |> Seq.exists (fun reference ->
                let treeNode = reference.GetTreeNode()
                let refExpr = treeNode.As<IReferenceExpr>()
                if isNull refExpr then false else
                isNotNull (SetExprNavigator.GetByLeftExpression(refExpr.IgnoreParentParens())))

        if hasWriteReferences references then Pair(false, "") else

        inlineExpr <- binding.Expression
        inlineReferences <- List(references)

        Pair(true, "")
