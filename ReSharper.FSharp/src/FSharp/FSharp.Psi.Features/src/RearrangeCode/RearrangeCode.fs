module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.RearrangeCode.RearrangeCode

open System
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.RearrangeCode
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

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
type FSharpRearrangeableElementSwap<'T when 'T: not struct and 'T :> ITreeNode>(node, title, direction) =
    inherit RearrangeableElementSwap<'T>(node, title, direction)

    abstract BeforeSwap: child: 'T * target: 'T -> unit
    default this.BeforeSwap(_, _) = ()

    override this.Swap(child, target) =
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

    let updateParens (pattern: IFSharpPattern) =
        match pattern with
        | :? IAsPat as asPat ->
            ParenPatUtil.addParensIfNeeded asPat

        | :? IParenPat as parenPat ->
            match parenPat.Pattern.IgnoreInnerParens() with
            | :? IAsPat as asPat ->
                let contextPattern = parenPat.IgnoreParentParens()
                if ParenPatUtil.needsParens contextPattern asPat then pattern else

                use writeLockCookie = WriteLockCookie.Create(pattern.IsPhysical())
                ModificationUtil.ReplaceChild(contextPattern, asPat) :> _
            | _ -> pattern

        | _ -> pattern

    override this.GetSiblings() =
        tuplePat.PatternsEnumerable :> _

    override this.Swap(child, target) =
        let newChild = base.Swap(child, target) |> updateParens

        let siblings = this.GetSiblings()
        CollectionUtil.GetPrevious(siblings, newChild) |> updateParens |> ignore
        CollectionUtil.GetNext(siblings, newChild) |> updateParens |> ignore
        newChild

[<RearrangeableElementType>]
type RearrangeableTuplePatternProvider() =
    inherit FSharpRearrangeableSingleElementBase<IFSharpPattern, ITuplePat>(TuplePatNavigator.GetByPattern,
        RearrangeableTuplePattern >> rearrangeable)

type RearrangeableOrPattern(fsPattern: IFSharpPattern, orPat: IOrPat) =
    inherit FSharpRearrangeableElementSwap<IFSharpPattern>(fsPattern, "or pattern", Direction.LeftRight)

    override this.GetSiblings() =
        [| orPat.Pattern1; orPat.Pattern2 |] :> _

[<RearrangeableElementType>]
type RearrangeableOrPatternProvider() =
    inherit FSharpRearrangeableSingleElementBase<IFSharpPattern, IOrPat>(OrPatNavigator.GetByPattern,
        RearrangeableOrPattern >> rearrangeable)



type RearrangeableAndPattern(fsPattern: IFSharpPattern, andsPat: IAndsPat) =
    inherit FSharpRearrangeableElementSwap<IFSharpPattern>(fsPattern, "and pattern", Direction.LeftRight)

    override this.GetSiblings() =
        andsPat.PatternsEnumerable :> _

[<RearrangeableElementType>]
type RearrangeableAndPatternProvider() =
    inherit FSharpRearrangeableSingleElementBase<IFSharpPattern, IAndsPat>(AndsPatNavigator.GetByPattern,
        RearrangeableAndPattern >> rearrangeable)


type RearrangeableTupleExpr(fsExpr: IFSharpExpression, tuplePat: ITupleExpr) =
    inherit FSharpRearrangeableElementSwap<IFSharpExpression>(fsExpr, "tuple expr", Direction.LeftRight)

    override this.GetSiblings() =
        tuplePat.ExpressionsEnumerable :> _

[<RearrangeableElementType>]
type RearrangeableTupleExprProvider() =
    inherit FSharpRearrangeableSingleElementBase<IFSharpExpression, ITupleExpr>(TupleExprNavigator.GetByExpression,
        RearrangeableTupleExpr >> rearrangeable)


[<RearrangeableElementType>]
type RearrangeableLambdaParamPatternProvider() =
    inherit FSharpRearrangeableSimpleSwap<IFSharpPattern, ILambdaExpr>(
        "lambda parameter", Direction.LeftRight, LambdaExprNavigator.GetByPattern,
        fun lambdaExpr -> lambdaExpr.PatternsEnumerable)


[<RearrangeableElementType>]
type RearrangeableRecordFieldDeclarationProvider() =
    inherit FSharpRearrangeableSimpleSwap<IRecordFieldDeclaration, IRecordFieldDeclarationList>(
        "record field declaration", Direction.All, RecordFieldDeclarationListNavigator.GetByFieldDeclaration,
        fun l -> l.FieldDeclarationsEnumerable)

[<RearrangeableElementType>]
type RearrangeableFunctionParameterProvider() =
    inherit FSharpRearrangeableSimpleSwap<IParametersPatternDeclaration, IBinding>(
        "function parameter", Direction.LeftRight, BindingNavigator.GetByParametersDeclaration,
        fun binding -> binding.ParametersDeclarationsEnumerable)


type RearrangeableEnumCaseLikeDeclaration(decl: IEnumCaseLikeDeclaration) =
    inherit FSharpRearrangeableElementSwap<IEnumCaseLikeDeclaration>(decl, "enum case like declaration", Direction.All)

    override this.GetSiblings() =
        EnumLikeTypeRepresentationNavigator.GetByEnumLikeCase(decl).NotNull().Cases :> _

    override this.BeforeSwap(child, target) =
        EnumCaseLikeDeclarationUtil.addBarIfNeeded child
        EnumCaseLikeDeclarationUtil.addBarIfNeeded target

[<RearrangeableElementType>]
type RearrangeableEnumCaseLikeDeclarationProvider() =
    inherit RearrangeableSingleElementBase<IEnumCaseLikeDeclaration>.TypeBase()

    override this.CreateElement(caseDeclaration: IEnumCaseLikeDeclaration): IRearrangeable =
        RearrangeableEnumCaseLikeDeclaration(caseDeclaration) :> _


type RearrangeableMatchClause(matchClause: IMatchClause, matchExpr: IMatchLikeExpr) =
    inherit FSharpRearrangeableElementSwap<IMatchClause>(matchClause, "match clause", Direction.All)

    let missingIndent (clause: IMatchClause) =
        let expr = clause.Expression
        isNotNull expr && clause.Indent = expr.Indent

    let removeIndent (clause: IMatchClause) (expr: IFSharpExpression) =
        do
            use disableFormatter = new DisableCodeFormatter()
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

        clause.FormatNode()

    override this.BeforeSwap(child, target) =
        MatchExprUtil.addIndent child
        MatchExprUtil.addIndent target

    override this.GetSiblings() =
        matchExpr.Clauses :> _

    override this.MoveUnderPsiTransaction(direction) =
        use writeLockCookie = WriteLockCookie.Create(matchClause.IsPhysical())

        // todo: comments

        match MatchExprUtil.isLastClause matchClause, matchClause.Expression, missingIndent matchClause, direction with
        | true, NotNull expr, _, Direction.Down ->
            removeIndent matchClause expr
            expr :> _

        | true, NotNull expr, true, Direction.Up ->
            MatchExprUtil.addIndent matchClause
            expr :> _

        | _ -> base.MoveUnderPsiTransaction(direction)

    override this.CanMove(direction) =
        base.CanMove(direction) ||

        match MatchExprUtil.isLastClause matchClause, matchClause.Expression, missingIndent matchClause, direction with
        | true, NotNull expr, _, Direction.Down ->
            let seqExpr = SequentialExprNavigator.GetByExpression(matchExpr)
            if isNotNull seqExpr && not (SequentialExprUtil.isLastExprInSeqExpr seqExpr matchExpr) then false else

            let matchExprStmt = ExpressionStatementNavigator.GetByExpression(matchExpr)
            let moduleDecl = ModuleDeclarationNavigator.GetByMember(matchExprStmt)
            let moduleMembers = if isNotNull moduleDecl then moduleDecl.Members else TreeNodeCollection.Empty
            if not moduleMembers.IsEmpty && matchExprStmt != moduleMembers.LastOrDefault() then false else

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
