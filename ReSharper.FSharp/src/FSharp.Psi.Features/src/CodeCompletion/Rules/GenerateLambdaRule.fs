namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText
open JetBrains.Util

module GenerateLambdaInfo =
    let [<Literal>] CreateLambda = "Create lambda"

    let needsParens (refExpr: IReferenceExpr) =
        // todo: escape tuple expressions, check expected types and position
        isNull (ParenOrBeginEndExprNavigator.GetByInnerExpression(refExpr))

type GenerateLambdaInfo(text, paramNames: string list list) =
    inherit TextualInfo(text, GenerateLambdaInfo.CreateLambda)

    member val Names = paramNames

    override this.IsRiderAsync = false


type GenerateLambdaBehavior(info: GenerateLambdaInfo) =
    inherit TextualBehavior<GenerateLambdaInfo>(info)

    override this.Accept(textControl, nameRange, _, _, solution, _) =
        let psiServices = solution.GetPsiServices()

        textControl.Document.ReplaceText(nameRange, "__")
        let nameRange = nameRange.StartOffset.ExtendRight("__".Length)

        psiServices.Files.CommitAllDocuments()
        let refExpr = TextControlToPsi.GetElement<IReferenceExpr>(solution, nameRange.EndOffset)

        use writeCookie = WriteLockCookie.Create(refExpr.IsPhysical())
        use transactionCookie =
            PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, GenerateLambdaInfo.CreateLambda)

        let text = if GenerateLambdaInfo.needsParens refExpr then $"({info.Text})" else info.Text
        let expr = refExpr.CreateElementFactory().CreateExpr(text)
        let insertedExpr = ModificationUtil.ReplaceChild(refExpr, expr)
        let lambdaExpr = insertedExpr.IgnoreInnerParens().As<ILambdaExpr>()

        let hotspotsRegistry = HotspotsRegistry(lambdaExpr.GetPsiServices())

        (info.Names, lambdaExpr.Patterns) ||> Seq.iter2 (fun names itemPattern ->
            let nameSuggestionsExpression = NameSuggestionsExpression(names)
            let rangeMarker = itemPattern.GetDocumentRange().CreateRangeMarker()
            hotspotsRegistry.Register(rangeMarker, nameSuggestionsExpression))

        let hotspotSession =
            LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
                solution, lambdaExpr.Expression.GetDocumentEndOffset(), textControl,
                LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotsRegistry.CreateHotspots())

        hotspotSession.ExecuteAndForget()


[<Language(typeof<FSharpLanguage>)>]
type GenerateLambdaRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion && not context.IsQualified

    override this.AddLookupItems(context, collector) =
        let productConfigurations = Shell.Instance.GetComponent<RunsProducts.ProductConfigurations>()
        if not (productConfigurations.IsInternalMode()) then false else

        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference then false else

        let referenceOwner = reference.GetElement().As<IReferenceExpr>()
        if isNull referenceOwner then false else

        match FSharpExpectedTypesUtil.tryGetExpectedFcsType referenceOwner with
        | None -> false
        | Some(expectedType, displayContext) ->

        if not expectedType.IsFunctionType then false else

        let fcsArgTypes = FcsTypeUtil.getFunctionTypeArgs false expectedType
        let lambdaParamTypes = fcsArgTypes |> List.map (fun f -> f.MapType(referenceOwner))

        let paramNames = 
            lambdaParamTypes
            |> List.map (fun t ->
                FSharpNamingService.createEmptyNamesCollection referenceOwner
                |> FSharpNamingService.addNamesForType t
                |> FSharpNamingService.prepareNamesCollection EmptySet.Instance referenceOwner
                |> (fun names -> List.ofSeq names @ ["_"]))

        let paramNamesText = 
            paramNames
            |> List.map (fun names ->
                names
                |> Seq.tryHead
                |> Option.defaultValue "_")
            |> String.concat " "

        let text =
            fcsArgTypes
            |> List.map (fun arg -> arg.Format(displayContext))
            |> String.concat " -> "

        let presentationText = $"fun {text} ->"
        let info = GenerateLambdaInfo($"fun {paramNamesText} -> ()", paramNames, Ranges = context.Ranges)

        let item =
            LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(fun _ -> TextualPresentation(RichText(presentationText), info) :> _)
                .WithBehavior(fun _ -> GenerateLambdaBehavior(info) :> _)
                .WithMatcher(fun _ -> TextualMatcher(presentationText, info) :> _)
                .WithRelevance(CLRLookupItemRelevance.ExpectedTypeMatchLambda)

        item.Placement.Location <- PlacementLocation.Top

        collector.Add(item)

        false
