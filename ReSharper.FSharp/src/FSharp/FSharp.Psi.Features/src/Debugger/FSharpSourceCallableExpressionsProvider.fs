namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Debugger

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Feature.Services.ExpressionSelection
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Util

type FSharpCallableExpressionsCollector private () =
    let isInvocation argCount (reference: FSharpSymbolReference) : bool =
        if isNull reference then false else

        let expectedArgCount: int option =
            match reference.GetFcsSymbol() with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                if mfv.IsValue then None else
                if mfv.IsProperty then Some 0 else

                Some mfv.CurriedParameterGroups.Count

            | :? FSharpUnionCase as unionCase when unionCase.HasFields -> Some 1
            | :? FSharpEntity as entity when entity.FSharpFields.Count > 0 -> Some 1

            | _ -> None

        match expectedArgCount, argCount with
        | Some 0, None -> true
        | Some expected, Some (_, actual) -> expected = actual
        | _ -> false

    let processInvokedReference (applicationInfo: (IFSharpExpression * int) option) (reference: FSharpSymbolReference)
            (result: List<_>) =
        if not (isInvocation applicationInfo reference) then () else

        match reference.Resolve().DeclaredElement with
        | :? ITypeMember as typeMember when
            let t = typeMember.ContainingType
            t.IsTuple() || t.IsValueTuple() -> ()

        | declaredElement ->
            let name =
                // todo: use compiled names
                let shortName = reference.GetName()
                match declaredElement with
                | :? IProperty -> "get_" + shortName
                | :? IConstructor -> ".ctor"
                | _ -> shortName

            let range = reference.GetDocumentRange()

            let presentationRange, exprText =
                match applicationInfo with
                | None -> range, reference.GetElement().GetText()
                | Some(appExpr, _) -> appExpr.GetDocumentRange(), appExpr.GetText()

            let expr = DocumentRangeExpression(presentationRange, name, exprText, range)
            result.Add(expr)

    static member Instance = FSharpCallableExpressionsCollector()

    member this.Process(expr: IFSharpExpression, result) =
        if isNotNull expr then
            expr.ProcessThisAndDescendants(this, result)

    interface IRecursiveElementProcessor<List<DocumentRangeExpression>> with
        member this.IsProcessingFinished(context) = false
        member this.ProcessAfterInterior(element, context) = ()

        member this.InteriorShouldBeProcessed(element, context) =
            match element with
            | :? IBinaryAppExpr
            | :? INewExpr
            | :? IPrefixAppExpr
            | :? IReferenceExpr -> false
            | _ -> true

        member this.ProcessBeforeInterior(element, result) =
            let expr = element.As<IFSharpExpression>()
            let expr = expr.IgnoreInnerParens()
            match expr with
            | :? IPrefixAppExpr as prefixAppExpr ->
                let qualifiedExpr = prefixAppExpr.FunctionExpression.As<IQualifiedExpr>()
                if isNotNull qualifiedExpr then
                    this.Process(qualifiedExpr.Qualifier, result)

                let argExpressions = prefixAppExpr.AppliedExpressions
                for argExpr in argExpressions do
                    this.Process(argExpr, result)

                let argCount = Some(prefixAppExpr :> IFSharpExpression, argExpressions.Count)
                processInvokedReference argCount prefixAppExpr.InvokedFunctionReference result

            | :? INewExpr as newExpr ->
                this.Process(newExpr.ArgumentExpression, result)
                processInvokedReference (Some(newExpr, 1)) newExpr.Reference result

            | :? IReferenceExpr as refExpr ->
                this.Process(refExpr.Qualifier, result)
                processInvokedReference None refExpr.Reference result

            | :? IBinaryAppExpr as binaryAppExpr ->
                this.Process(binaryAppExpr.LeftArgument, result)
                this.Process(binaryAppExpr.RightArgument, result)
                this.Process(binaryAppExpr.Operator, result)

            | _ -> ()

[<Language(typeof<FSharpLanguage>)>]
type FSharpSourceCallableExpressionsProvider() =
    let isReturnOrYield (tokenNode: ITokenNode) =
        let tokenType = getTokenType tokenNode

        tokenType == FSharpTokenType.RETURN ||
        tokenType == FSharpTokenType.RETURN_BANG ||
        tokenType == FSharpTokenType.YIELD ||
        tokenType == FSharpTokenType.YIELD_BANG

    let getMatchBranchExprs (matchExpr: IMatchExpr) : ITreeNode list =
        if isNull matchExpr then [] else

        matchExpr.ClausesEnumerable
        |> Seq.collect (fun clause -> [clause.WhenExpression :> ITreeNode; clause.Expression])
        |> Seq.toList

    let getNextExpr (expr: IFSharpExpression) (seqExpr: ISequentialExpr) : ITreeNode =
        let exprs = seqExpr.Expressions
        let exprIndex = exprs.IndexOf(expr)
        let fSharpExpressionOption = exprs |> Seq.tryItem (exprIndex + 1)
        fSharpExpressionOption |> Option.defaultValue null :> _

    let getPossibleNextExpressions (node: ITreeNode) =
        let result = List<ITreeNode>()

        let mutable shouldContinue = true
        for parent in node.ContainingNodes(true) do
            if not shouldContinue then () else

            let expr = parent.As<IFSharpExpression>()
            if isNull expr then () else

            match expr with
            | :? IComputationExpr -> shouldContinue <- false
            | :? IForEachExpr as forExpr -> result.AddRange([forExpr.ForKeyword; forExpr.DoExpression])
            | _ ->

            let ifExpr = IfExprNavigator.GetByConditionExpr(expr)
            if isNotNull ifExpr then
                result.AddRange([ifExpr.ThenExpr; ifExpr.ElseExpr]) else

            let matchExpr = MatchExprNavigator.GetByExpression(expr)
            if isNotNull matchExpr then
                result.AddRange(getMatchBranchExprs matchExpr) else

            let matchClause = MatchClauseNavigator.GetByWhenExpression(expr)
            let matchExpr = MatchExprNavigator.GetByClause(matchClause)
            if isNotNull matchExpr then
                result.AddRange(getMatchBranchExprs matchExpr) else

            let seqExpr = SequentialExprNavigator.GetByExpression(expr)
            if isNotNull seqExpr then
                result.Add(getNextExpr expr seqExpr) else

            let whileExpr = WhileExprNavigator.GetByExpression(expr)
            if isNotNull whileExpr then
                result.AddRange([whileExpr.ConditionExpr; whileExpr.DoExpression]) else

            ()

        result

    let expressionSelectionProvider =
        { new ExpressionSelectionProviderBase<IFSharpExpression>() with
            override this.IsTokenSkipped(token) =
                let tokenType = getTokenType token
                isNotNull tokenType && tokenType.IsKeyword ||

                base.IsTokenSkipped(token) }

    interface ISourceCallableExpressionsProvider with
        member this.GetExpressionList(file, solution, startLine, startCol, endLine, endCol) =
            let sourceFile = file.ToSourceFile()
            if isNull sourceFile then null else

            let startCoords = DocumentCoords(docLine (startLine - 1), docColumn (startCol - 1))
            let endCoords = DocumentCoords(docLine (endLine - 1), docColumn (endCol - 1))

            let document = sourceFile.Document
            let startOffset = document.GetOffsetByCoordsSafe startCoords
            let endOffset = document.GetOffsetByCoordsSafe endCoords
            if not startOffset.HasValue || not endOffset.HasValue then null else

            let fsFile = sourceFile.GetPrimaryPsiFile().AsFSharpFile()
            if isNull fsFile then null else

            let expr =
                let startOffset = DocumentOffset(document, startOffset.Value)
                let endOffset = DocumentOffset(document, endOffset.Value)

                let node = fsFile.FindNodeAt(DocumentRange(&startOffset, &endOffset))
                match node with
                | :? IFSharpExpression as expr -> expr
                | :? IBinding as binding -> binding.Expression

                // Workaround dotnet/fsharp#19248
                | :? ITokenNode as tokenNode when isReturnOrYield tokenNode ->
                    tokenNode.Parent.As<IFSharpExpression>()

                | _ -> null

            if isNull expr then null else

            let result = List()
            expr.ProcessThisAndDescendants(FSharpCallableExpressionsCollector.Instance, result)
            result

        member this.GetAdditionalResumeLocations(location) =
            let projectFile = location.ProjectFile
            let sourceFile = projectFile.ToSourceFile()
            if isNull sourceFile then null else

            let startCoords = DocumentCoords(docLine (location.StartLine - 1), docColumn (location.StartCol - 1))
            let endCoords = DocumentCoords(docLine (location.EndLine - 1), docColumn (location.EndCol - 1))

            let document = sourceFile.Document
            let startOffset = document.GetOffsetByCoordsSafe startCoords
            let endOffset = document.GetOffsetByCoordsSafe endCoords
            if not startOffset.HasValue || not endOffset.HasValue then null else

            let fsFile = sourceFile.GetPrimaryPsiFile().AsFSharpFile()
            if isNull fsFile then null else

            let startOffset = DocumentOffset(document, startOffset.Value)
            let endOffset = DocumentOffset(document, endOffset.Value)
            let range = DocumentRange(&startOffset, &endOffset)
            
            let node = fsFile.FindNodeAt(range)
            if isNull node then null else

            let result = List()
            let nextExprs = getPossibleNextExpressions node
            for expr in nextExprs do
                if isNotNull expr then
                    result.Add(SequencePointLocation(projectFile, int expr.StartLine, expr.Indent))

            result

        member this.GetCallableSource(file, solution, startLine, startCol, endLine, endCol, callIndex, callableName) = ""
        member this.GetExpressionRange(file, solution, startLine, startCol) = DocumentRange.InvalidRange
