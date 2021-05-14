module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.RearrangeCode.RearrangeCode

open System
open System.Runtime.InteropServices
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.RearrangeCode
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell

type FSharpRearrangeableSimpleSwap<'TNode, 'TParent when
        'TNode: not struct and 'TNode :> ITreeNode and
        'TParent: not struct and 'TParent :> ITreeNode>(title, direction, parentFunc,
            childrenFunc: 'TParent -> TreeNodeEnumerable<'TNode>) =
    inherit RearrangeableSimpleSwap<'TNode>.SimpleSwapType<'TParent>(title, direction, Func<_,_>(parentFunc),
        Func<_,_>(childrenFunc >> Seq.cast))


[<RearrangeableElementType>]
type RearrangeableCaseFieldDeclarationProvider() =
    inherit FSharpRearrangeableSimpleSwap<ICaseFieldDeclaration, IUnionCaseFieldDeclarationList>(
        "case field declaration", Direction.LeftRight, UnionCaseFieldDeclarationListNavigator.GetByField,
        fun l -> l.FieldsEnumerable)


[<AbstractClass>]
type FSharpRearrangeableElementSwap<'T when 'T: not struct and 'T :> ITreeNode>(node, title, direction,
        [<Optional; DefaultParameterValue(true)>] enableFormatter) =
    inherit RearrangeableElementSwap<'T>(node, title, direction)

    abstract BeforeSwap: child: 'T * target: 'T -> unit
    default this.BeforeSwap(_, _) = ()

    override this.Swap(child, target) =
        if enableFormatter then
            use enableFormatterCookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
            this.BeforeSwap(child, target)
            base.Swap(child, target)
        else
            this.BeforeSwap(child, target)
            base.Swap(child, target)


type FSharpRearrangeableSingleElementBase<'TNode, 'TParent when
        'TNode: not struct and 'TNode: null and 'TNode :> ITreeNode and
        'TParent: not struct and 'TParent: null and 'TParent :> ITreeNode>(parentGetter: 'TNode -> 'TParent,
            elementCreator: 'TNode * 'TParent -> IRearrangeable) =
    inherit RearrangeableSingleElementBase<'TNode>.TypeBase()

    override this.CreateElement(node: 'TNode): IRearrangeable =
        match parentGetter node with
        | null -> null
        | parent -> elementCreator (node, parent)

let rearrangeable (value: #IRearrangeable) =
    value :> IRearrangeable


type RearrangeableTuplePattern(fsPattern: IFSharpPattern, tuplePat: ITuplePat) =
    inherit FSharpRearrangeableElementSwap<IFSharpPattern>(fsPattern, "tuple pattern", Direction.LeftRight)

    override this.GetSiblings() =
        tuplePat.PatternsEnumerable :> _

[<RearrangeableElementType>]
type RearrangeableTuplePatternProvider() =
    inherit FSharpRearrangeableSingleElementBase<IFSharpPattern, ITuplePat>(TuplePatNavigator.GetByPattern,
        RearrangeableTuplePattern >> rearrangeable)


type RearrangeableTupleExpr(fsExpr: IFSharpExpression, tuplePat: ITupleExpr) =
    inherit FSharpRearrangeableElementSwap<IFSharpExpression>(fsExpr, "tuple expr", Direction.LeftRight)

    override this.GetSiblings() =
        tuplePat.ExpressionsEnumerable :> _

[<RearrangeableElementType>]
type RearrangeableTupleExprProvider() =
    inherit FSharpRearrangeableSingleElementBase<IFSharpExpression, ITupleExpr>(TupleExprNavigator.GetByExpression,
        RearrangeableTupleExpr >> rearrangeable)


[<RearrangeableElementType>]
type RearrangeableRecordFieldDeclarationProvider() =
    inherit FSharpRearrangeableSimpleSwap<IRecordFieldDeclaration, IRecordFieldDeclarationList>(
        "record field declaration", Direction.All, RecordFieldDeclarationListNavigator.GetByFieldDeclaration,
        fun l -> l.FieldDeclarationsEnumerable)


type RearrangeableEnumCaseLikeDeclaration(decl: IEnumCaseLikeDeclaration) =
    inherit FSharpRearrangeableElementSwap<IEnumCaseLikeDeclaration>(decl, "enum case like declaration", Direction.All,
        false)

    let addBarIfNeeded (caseDeclaration: IEnumCaseLikeDeclaration) =
        if isNull caseDeclaration.Bar && isNotNull caseDeclaration.FirstChild then
            use cookie = WriteLockCookie.Create(caseDeclaration.IsPhysical())
            addNodesBefore caseDeclaration.FirstChild [
                FSharpTokenType.BAR.CreateLeafElement()
                Whitespace()
            ] |> ignore

    override this.GetSiblings() =
        match decl with
        | :? IUnionCaseDeclaration as caseDeclaration ->
            UnionRepresentationNavigator.GetByUnionCase(caseDeclaration).NotNull().UnionCases |> Seq.cast

        | :? IEnumCaseDeclaration as caseDeclaration ->
            EnumRepresentationNavigator.GetByEnumCase(caseDeclaration).NotNull().EnumCases |> Seq.cast

        | _ -> failwithf $"Unexpected declaration: {decl}"

    override this.BeforeSwap(child, target) =
        addBarIfNeeded child
        addBarIfNeeded target

[<RearrangeableElementType>]
type RearrangeableEnumCaseLikeDeclarationProvider() =
    inherit RearrangeableSingleElementBase<IEnumCaseLikeDeclaration>.TypeBase()

    override this.CreateElement(caseDeclaration: IEnumCaseLikeDeclaration): IRearrangeable =
        RearrangeableEnumCaseLikeDeclaration(caseDeclaration) :> _


type RearrangeableMatchClause(matchClause: IMatchClause, matchExpr: IMatchLikeExpr) =
    inherit FSharpRearrangeableElementSwap<IMatchClause>(matchClause, "match clause", Direction.All)

    let isLastClause (clause: IMatchClause) =
        clause == matchExpr.Clauses.Last()

    let missingIndent (clause: IMatchClause) =
        let expr = clause.Expression
        isNotNull expr && clause.Indent = expr.Indent

    let addIndent (clause: IMatchClause) =
        if not (isLastClause clause) then () else

        let expr = clause.Expression
        if isNull expr || expr.Indent <> clause.Indent then () else

        if expr.IsSingleLine then
            let oldRange = TreeRange(clause.RArrow.NextSibling, expr.PrevSibling)
            ModificationUtil.ReplaceChildRange(oldRange, TreeRange(Whitespace())) |> ignore
        else
            if clause.RArrow.StartLine.Plus1() < expr.StartLine then
                let lineEnding = expr.GetLineEnding()
                let oldRange = TreeRange(clause.RArrow.NextSibling, expr.PrevSibling)
                ModificationUtil.DeleteChildRange(oldRange)
                ModificationUtil.AddChildAfter(clause.RArrow, Whitespace(clause.Indent)) |> ignore
                ModificationUtil.AddChildAfter(clause.RArrow, NewLine(lineEnding)) |> ignore

            let indentSize = expr.GetIndentSize()
            shiftWithWhitespaceBefore indentSize expr

    let removeIndent (clause: IMatchClause) (expr: IFSharpExpression) =
        let clauseIndent = clause.Indent
        let indentDiff = expr.Indent - clauseIndent

        shiftWithWhitespaceBefore -indentDiff expr

        let arrow = clause.RArrow
        let arrowStartLine = arrow.StartLine
        let exprStartLine = expr.StartLine

        if arrowStartLine.Plus1() >= exprStartLine then
            let lineEnding = expr.GetLineEnding()
            addNodesAfter arrow [
                NewLine(lineEnding)
                if arrowStartLine = exprStartLine then
                    NewLine(lineEnding)
                    Whitespace(clauseIndent)
            ] |> ignore
    
    override this.BeforeSwap(child, target) =
        addIndent child
        addIndent target

    override this.GetSiblings() =
        matchExpr.Clauses :> _

    override this.MoveUnderPsiTransaction(direction) =
        use writeLockCookie = WriteLockCookie.Create(matchClause.IsPhysical())

        // todo: comments

        match isLastClause matchClause, matchClause.Expression, missingIndent matchClause, direction with
        | true, NotNull expr, _, Direction.Down ->
            removeIndent matchClause expr
            expr :> _

        | true, NotNull expr, true, Direction.Up ->
            addIndent matchClause
            expr :> _

        | _ -> base.MoveUnderPsiTransaction(direction)

    override this.CanMove(direction) =
        base.CanMove(direction) ||

        match isLastClause matchClause, matchClause.Expression, missingIndent matchClause, direction with
        | true, NotNull expr, _, Direction.Down ->
            let arrow = matchClause.RArrow
            isNotNull arrow && arrow.StartLine.Plus1() >= expr.StartLine
        | true, NotNull _, true, Direction.Up -> true
        | _ -> false

[<RearrangeableElementType>]
type RearrangeableMatchClauseProvider() =
    inherit RearrangeableSingleElementBase<IMatchClause>.TypeBase()

    override this.CreateElement(clause: IMatchClause): IRearrangeable =
        let matchExpr = MatchExprNavigator.GetByClause(clause)
        if isNull matchExpr then null else

        RearrangeableMatchClause(clause, matchExpr) :> _
