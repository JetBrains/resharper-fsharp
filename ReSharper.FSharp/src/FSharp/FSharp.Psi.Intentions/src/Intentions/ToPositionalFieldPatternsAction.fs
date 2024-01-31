namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "ToPositionalFieldPatternsAction", GroupType = typeof<FSharpContextActions>,
                Description = "Match union case fields explicitly")>]
type ToPositionalFieldPatternsAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let getUnionCasePatterns () =
        let referenceName = dataProvider.GetSelectedElement<IExpressionReferenceName>()
        let fieldPat = FieldPatNavigator.GetByReferenceName(referenceName)
        let unionCaseFieldsPat = NamedUnionCaseFieldsPatNavigator.GetByFieldPattern(fieldPat)
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(unionCaseFieldsPat)
        unionCaseFieldsPat, parametersOwnerPat

    override this.Text = "Use positional field patterns"

    override this.IsAvailable _ =
        let unionCaseFieldsPat, parametersOwnerPat = getUnionCasePatterns ()
        if isNull parametersOwnerPat then false else
        if not parametersOwnerPat.IsSingleLine || parametersOwnerPat.Parameters.Count > 1 then false else

        let fcsUnionCase = parametersOwnerPat.Reference.GetFcsSymbol().As<FSharpUnionCase>()
        if isNull fcsUnionCase || fcsUnionCase.Fields.Count < 1 then false else

        unionCaseFieldsPat.FieldPatterns
        |> Seq.exists (fun pat ->
            match pat.Reference with
            | null -> false
            | reference -> reference.GetFcsSymbol() |> isNotNull
        )

    override this.ExecutePsiTransaction(_, _) =
        let unionCaseFieldsPat, parametersOwnerPat = getUnionCasePatterns ()

        use writeCookie = WriteLockCookie.Create(parametersOwnerPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        
        let fcsUnionCase = parametersOwnerPat.Reference.GetFcsSymbol() :?> FSharpUnionCase

        let existingPatterns = Dictionary()
        for fieldPat in unionCaseFieldsPat.FieldPatterns do
            existingPatterns[fieldPat.ReferenceName.ShortName] <- fieldPat.Pattern.IgnoreInnerParens()

        let components =
            let fcsEntityInstance = FcsEntityInstance.create fcsUnionCase.ReturnType
            FSharpDeconstructionImpl.createUnionCaseFields parametersOwnerPat fcsUnionCase fcsEntityInstance

        let usedNames = FSharpNamingService.getPatternContextUsedNames parametersOwnerPat
        let pattern, names = FSharpDeconstructionImpl.createInnerPattern parametersOwnerPat components false usedNames
        
        let replacedPat =
            ModificationUtil.ReplaceChild(unionCaseFieldsPat.IgnoreParentParens(), pattern)
            |> ParenPatUtil.addParensIfNeeded

        let patterns =
            match replacedPat with
            | :? ITuplePat as tuplePat -> tuplePat.Patterns
            | _ -> TreeNodeCollection [| replacedPat |]

        let hotspotsRegistry = HotspotsRegistry(parametersOwnerPat.GetPsiServices())

        for fieldComponent, names, itemPat in (components, names, patterns)|||> Seq.zip3 do
            let fieldComponent = fieldComponent :?> SingleValueDeconstructionComponent
            match tryGetValue fieldComponent.Name existingPatterns with
            | Some pat ->
                ModificationUtil.ReplaceChild(itemPat, pat) |> ignore

            | None ->
                let nameSuggestionsExpression = NameSuggestionsExpression(names)
                let rangeMarker = itemPat.GetDocumentRange().CreateRangeMarker()
                hotspotsRegistry.Register(rangeMarker, nameSuggestionsExpression)

        Action<_>(fun textControl ->
            BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, replacedPat.GetDocumentEndOffset()).Invoke(textControl)
        )
