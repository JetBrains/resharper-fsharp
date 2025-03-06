namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "FunctionToMatchInBindingAction", GroupType = typeof<FSharpContextActions>,
                Description = "Introduce parameter for function expression")>]
type MatchLambdaExprToParameterAction(dataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let getMfv (binding: IBinding) =
        let refPat = binding.HeadPattern.As<IReferencePat>()
        if isNull refPat then Unchecked.defaultof<_> else

        refPat.GetFcsSymbol().As<FSharpMemberOrFunctionOrValue>()

    override this.Text = "Introduce and match parameter"

    override this.IsAvailable _ =
        let matchLambdaExpr = dataProvider.GetSelectedElement<IMatchLambdaExpr>()
        let binding = BindingNavigator.GetByExpression(matchLambdaExpr)
        isNotNull binding &&

        this.IsAtTreeNode(matchLambdaExpr.FunctionKeyword)

    override this.ExecutePsiTransaction(_, _) =
        let matchLambdaExpr = dataProvider.GetSelectedElement<IMatchLambdaExpr>()
        let binding = BindingNavigator.GetByExpression(matchLambdaExpr)

        let elementFactory = binding.CreateElementFactory()
        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())

        let mfv = getMfv binding
        if isNull mfv then null else

        let parameterTypes = FcsTypeUtil.getFunctionTypeArgs true mfv.FullType
        let parameterPatternCount = binding.ParameterPatterns.Count

        match parameterTypes |> Seq.skip parameterPatternCount |> Seq.tryHead with
        | None -> null
        | Some nextParameterType ->

        let usedNames = FSharpNamingService.getBindingPatternsUsedNames binding

        let names =
            FSharpNamingService.createEmptyNamesCollection binding
            |> FSharpNamingService.addNamesForFcsType binding nextParameterType
            |> FSharpNamingService.prepareNamesCollection usedNames binding

        let names = List.ofSeq names
        let name = if names.Length > 1 then names[0] else "x"

        let parameterDecl =
            let prevSibling = binding.EqualsToken.GetPreviousMeaningfulSibling()
            let paramDecl = ElementType.PARAMETERS_PATTERN_DECLARATION.Create()
            ModificationUtil.AddChildAfter(prevSibling, paramDecl)

        let parameterDecl = parameterDecl.As<IParametersPatternDeclaration>()
        let pat = elementFactory.CreatePattern(name, false)
        parameterDecl.SetPattern(pat) |> ignore

        moveCommentsAndWhitespaceAfterAnchor binding.EqualsToken matchLambdaExpr.FunctionKeyword

        let matchExpr = elementFactory.CreateExpr($"match {name} with | _ -> ()") :?> IMatchExpr
        let generatedClausesRange = TreeRange(matchExpr.WithKeyword.NextSibling, matchExpr.LastChild)

        let clauses = matchLambdaExpr.Clauses
        let originalClausesRange = TreeRange(clauses[0] |> getFirstMatchingNodeBefore isInlineSpaceOrComment |> getThisOrPrevNewLine, clauses.Last())

        ModificationUtil.ReplaceChildRange(generatedClausesRange, originalClausesRange) |> ignore
        let matchExpr = ModificationUtil.ReplaceChild(matchLambdaExpr, matchExpr)

        let hotspotsRegistry = HotspotsRegistry(matchExpr.GetPsiServices())
        let nodes: ITreeNode list = [parameterDecl; matchExpr.Expression]
        hotspotsRegistry.Register(nodes, NameSuggestionsExpression(names))

        Action<_>(fun texControl ->
            let offset = parameterDecl.GetDocumentEndOffset()
            BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, offset).Invoke(texControl)
        )
