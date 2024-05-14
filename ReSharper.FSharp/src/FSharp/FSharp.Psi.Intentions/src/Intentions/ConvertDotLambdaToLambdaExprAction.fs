namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<ContextAction(Name = "ConvertDotLambdaToLambdaExpr", GroupType = typeof<FSharpContextActions>,
                Description = "Converts shorthand lambda expression to a lambda expression")>]
type ConvertDotLambdaToLambdaExprAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let rec getQualifierReferenceExpr (expr: IFSharpExpression) =
        match expr with
        | :? IReferenceExpr as refExpr ->
            match refExpr.Qualifier with
            | null -> refExpr
            | qualifierExpr -> getQualifierReferenceExpr qualifierExpr

        | :? IPrefixAppExpr as appExpr ->
            getQualifierReferenceExpr appExpr.FunctionExpression

        | _ -> null

    override this.Text = "To lambda expression"

    override this.IsAvailable _ =
        let dotLambdaExpr = dataProvider.GetSelectedElement<IDotLambdaExpr>()
        isNotNull dotLambdaExpr && dotLambdaExpr.IsSingleLine &&

        let shorthand = dotLambdaExpr.Shorthand
        isNotNull shorthand &&

        let refExpr = getQualifierReferenceExpr dotLambdaExpr.Expression

        let range =
            DisjointedTreeTextRange.From(shorthand)
                .Then(dotLambdaExpr.Delimiter)
                .Then(refExpr)

        range.Contains(dataProvider.SelectedTreeRange) &&

        let paramType = shorthand.GetExpressionTypeFromFcs()
        not paramType.IsUnknown

    override this.ExecutePsiTransaction(_, _) =
        let dotLambdaExpr = dataProvider.GetSelectedElement<IDotLambdaExpr>()
        let refExpr = getQualifierReferenceExpr dotLambdaExpr.Expression

        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let names =
            let apparentType = dotLambdaExpr.Shorthand.GetExpressionTypeFromFcs()
            let usedNames = FSharpNamingService.getUsedNames [dotLambdaExpr.Expression] EmptyList.Instance null false

            FSharpNamingService.createEmptyNamesCollection refExpr
            |> FSharpNamingService.addNamesForType apparentType
            |> FSharpNamingService.prepareNamesCollection usedNames refExpr

        let factory = refExpr.CreateElementFactory()
        let name = names[0]

        addNodesBefore refExpr.FirstChild [
            factory.CreateReferenceExpr(name)
            FSharpTokenType.DOT.CreateLeafElement()
        ] |> ignore

        let lambdaExpr = factory.CreateExpr($"fun {name} -> x") :?> ILambdaExpr
        lambdaExpr.SetExpression(dotLambdaExpr.Expression) |> ignore

        let lambdaExpr = ModificationUtil.ReplaceChild(dotLambdaExpr, lambdaExpr)
        let lambdaExpr = addParensIfNeeded lambdaExpr :?> ILambdaExpr
        let bodyExpr = lambdaExpr.Expression

        let hotspotsRegistry = HotspotsRegistry(lambdaExpr.GetPsiServices())
        let pattern = lambdaExpr.PatternsEnumerable |> Seq.head
        let qualifierExpr = getQualifierReferenceExpr bodyExpr
        let nodes = [pattern :> ITreeNode; qualifierExpr]
        hotspotsRegistry.Register(nodes, NameSuggestionsExpression(names))

        Action<_>(fun texControl ->
            let offset = bodyExpr.GetDocumentStartOffset()
            BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, offset).Invoke(texControl)
        )
