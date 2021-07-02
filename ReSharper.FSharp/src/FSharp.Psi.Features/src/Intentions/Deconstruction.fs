namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Progress
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpSymbolUtil
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Search
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util.Deconstruction
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type SingleValueDeconstructionComponent(name: string, additionalNames: string list, valueType: IType) =
    member val Name = name
    member val AdditionalNames = additionalNames
    interface IDeconstructionComponent with
        member this.Type = valueType

type DeconstructionFromTuple(pattern: IFSharpPattern, components: IDeconstructionComponent list) =
    member val Pattern = pattern
    member val Components = components

    interface IDeconstruction with
        member this.Components = this.Components :> _
        member this.Type = TypeFactory.CreateUnknownType(this.Pattern.GetPsiModule()) :> _

type DeconstructionFromUnionCaseFields(pattern: IParametersOwnerPat, components: IDeconstructionComponent list) =
    member val Pattern = pattern
    member val Components = components

    interface IDeconstruction with
        member this.Components = this.Components :> _
        member this.Type = TypeFactory.CreateUnknownType(this.Pattern.GetPsiModule()) :> _

type DeconstructionFromUnionCase(name: string, pattern: IFSharpPattern, components: IDeconstructionComponent list) =
    member val Name = name
    member val Pattern = pattern
    member val Components = components

    interface IDeconstruction with
        member this.Components = this.Components :> _
        member this.Type = TypeFactory.CreateUnknownType(this.Pattern.GetPsiModule()) :> _


type DeconstructAction(deconstruction: IDeconstruction) =
    inherit BulbActionBase()

    let hasUsages (pat: IFSharpPattern) =
        match pat with
        | :? IReferencePat as refPat ->
            let element = refPat.DeclaredElement
            let references = List()
            let searchPattern = SearchPattern.FIND_USAGES ||| SearchPattern.FIND_RELATED_ELEMENTS
            let searchDomain = element.GetSearchDomain()
            pat.GetPsiServices().AsyncFinder.Find([| element |], searchDomain, references.ConsumeReferences(),
                searchPattern, NullProgressIndicator.Create())

            references.Any()

        | _ -> false

    let createPattern (pat: IFSharpPattern) (components: IDeconstructionComponent list) =
        let binding = pat.GetBinding()
        let factory = pat.CreateElementFactory()

        let inExprs =
            let letExpr = LetBindingsNavigator.GetByBinding(binding.As()).As<ILetOrUseExpr>()
            if isNotNull letExpr then [letExpr.InExpression] else

            let pat = skipIntermediatePatParents pat

            let matchClause = MatchClauseNavigator.GetByPattern(pat)
            if isNotNull matchClause then [matchClause.WhenExpression; matchClause.Expression] else

            let lambdaExpr = LambdaExprNavigator.GetByPattern(pat)
            if isNotNull lambdaExpr then [lambdaExpr.Expression] else

            let memberDeclaration = MemberDeclarationNavigator.GetByParameterPattern(pat)
            if isNotNull memberDeclaration then [memberDeclaration.Expression] else

            []

        let containingType = FSharpNamingService.getPatternContainingType pat
        let usedNames = FSharpNamingService.getUsedNames inExprs EmptyList.InstanceList containingType false

        let hasUsages = 
            match pat, hasUsages pat with
            | :? IReferencePat as refPat, true ->
                usedNames.Add(refPat.SourceName) |> ignore
                true

            | :? IReferencePat as refPat, false ->
                usedNames.Remove(refPat.SourceName) |> ignore
                false

            | _ -> false

        let isSingle = components.Length = 1

        let names =
            components
            |> List.mapi (fun i tupleComponent ->
                let defaultItemName = if isSingle then "Item" else $"Item{i + 1}"
                FSharpNamingService.createEmptyNamesCollection pat
                |> (fun namesCollection ->
                    match tupleComponent with
                    | :? SingleValueDeconstructionComponent as valueComponent ->
                        let name = valueComponent.Name
                        if name <> defaultItemName then
                            FSharpNamingService.addNames name pat namesCollection |> ignore
                    | _ -> ()
                    namesCollection)
                |> FSharpNamingService.addNamesForType tupleComponent.Type
                |> (fun namesCollection ->
                    match tupleComponent with
                    | :? SingleValueDeconstructionComponent as valueComponent ->
                        (namesCollection, valueComponent.AdditionalNames) ||> List.fold (fun _ name ->
                            FSharpNamingService.addNames name pat namesCollection)
                    | _ -> namesCollection)
                |> FSharpNamingService.prepareNamesCollection usedNames pat
                |> fun names ->
                    let names = List.ofSeq names @ [if isSingle then "item" else $"item{i + 1}"; "_"]
                    usedNames.Add(names.Head) |> ignore
                    List.distinct names)

        let isTopLevel = binding :? ITopBinding
        let patternText = names |> List.map List.head |> String.concat ", "
        let pattern = factory.CreatePattern(patternText, isTopLevel)

        if hasUsages then
            let refPat = pat :?> IReferencePat
            let asPat = factory.CreatePattern($"_ as {refPat.SourceName}", isTopLevel) :?> IAsPat
            asPat.SetPattern(pattern) |> ignore
            asPat :> IFSharpPattern, names
        else
            pattern, names

    override this.ExecutePsiTransaction(_, _) =
        let pat: IFSharpPattern =
            match deconstruction with
            | :? DeconstructionFromTuple as tupleDeconstruction -> tupleDeconstruction.Pattern
            | :? DeconstructionFromUnionCaseFields as tupleDeconstruction -> tupleDeconstruction.Pattern :> _
            | :? DeconstructionFromUnionCase as tupleDeconstruction -> tupleDeconstruction.Pattern
            | _ -> failwith "todo"

        use writeCookie = WriteLockCookie.Create(pat.IsPhysical())
        let factory = pat.CreateElementFactory()
        let hotspotsRegistry = HotspotsRegistry(pat.GetPsiServices())

        let pattern, names =
            match deconstruction with
            | :? DeconstructionFromTuple as tupleDeconstruction ->
                let pat = tupleDeconstruction.Pattern
                let pattern, names = createPattern pat tupleDeconstruction.Components
                ModificationUtil.ReplaceChild(pat, pattern), names

            | :? DeconstructionFromUnionCaseFields as unionCaseDeconstruction ->
                let pat = unionCaseDeconstruction.Pattern
                let pattern, names = createPattern pat unionCaseDeconstruction.Components
                ModificationUtil.ReplaceChild(pat.Parameters.[0], pattern), names

            | :? DeconstructionFromUnionCase as unionCaseDeconstruction ->
                let pat = unionCaseDeconstruction.Pattern
                let pattern, names = createPattern pat unionCaseDeconstruction.Components
                let name = unionCaseDeconstruction.Name
                let parametersOwnerPat =
                    let pattern = factory.CreatePattern($"({name} _)", false) :?> IParenPat
                    pattern.Pattern :?> IParametersOwnerPat

                let parametersOwnerPat = ModificationUtil.ReplaceChild(pat, parametersOwnerPat)
                ModificationUtil.ReplaceChild(parametersOwnerPat.Parameters.[0], pattern), names

            | _ -> null, Unchecked.defaultof<_>

        let pattern: IFSharpPattern =
            let contextPattern = pattern.IgnoreParentParens()
            if contextPattern == pattern && RedundantParenPatAnalyzer.needsParens contextPattern pattern then
                let parenPattern = factory.CreatePattern("(_)", false) :?> IParenPat
                let patternCopy = pattern.Copy()
                let parenPattern = ModificationUtil.ReplaceChild(pattern, parenPattern)
                parenPattern.SetPattern(patternCopy)
            else
                pattern

        let itemPatterns: seq<IFSharpPattern> =
            match pattern with
            | :? IAsPat as asPat -> asPat.Pattern.As<ITuplePat>().PatternsEnumerable :> _
            | :? ITuplePat as tuplePat -> tuplePat.PatternsEnumerable :> _
            | :? IReferencePat as refPat -> Seq.singleton refPat |> Seq.cast
            | _ -> invalidOp $"Unexpected pattern: {pattern}"

        (names, itemPatterns) ||> Seq.iter2 (fun names itemPattern ->
            let nameSuggestionsExpression = NameSuggestionsExpression(names)
            let rangeMarker = itemPattern.GetDocumentRange().CreateRangeMarker()
            hotspotsRegistry.Register(rangeMarker, nameSuggestionsExpression))

        BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, DocumentOffset.InvalidOffset)

    override this.Text =
        match deconstruction with
        | :? DeconstructionFromTuple -> "Deconstruct tuple"
        | :? DeconstructionFromUnionCaseFields -> "Deconstruct union case fields"
        | :? DeconstructionFromUnionCase as d -> $"Deconstruct '{d.Name}' union case"
        | _ -> invalidOp $"Unexpected deconstruction: {deconstruction}"


[<ContextAction(Name = "Deconstruct variable",
                Description = "Deconstructs pattern into multiple positional components")>]
type DeconstructPatternAction(provider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(provider)

    let getPattern () =
        let wildPat = provider.GetSelectedElement<IWildPat>()
        if isNotNull wildPat then wildPat :> IFSharpPattern else

        let refPat = provider.GetSelectedElement<IReferencePat>()
        let binding = BindingNavigator.GetByHeadPattern(refPat)
        if isNotNull binding && binding.ParametersDeclarationsEnumerable.Any() then null else

        refPat :> _

    let isApplicablePattern (pat: IFSharpPattern) =
        let binding = BindingNavigator.GetByHeadPattern(pat)
        if isNotNull binding && binding.ParametersDeclarationsEnumerable.Any() then false else

        if isNotNull (ConstructorDeclarationNavigator.GetByParameterPatterns(skipIntermediatePatParents pat)) then false else

        if isNull (pat.GetPartialDeclarations().SingleItem()) then false else

        true

    let getPatternFcsType (pat: IFSharpPattern) =
        match pat with
        | :? IWildPat as wildPat -> wildPat.TryGetFcsType()
        | :? IReferencePat as refPat ->
            match refPat.GetFcsSymbol() with
            | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.FullType
            | _ -> Unchecked.defaultof<_>
        | _ -> Unchecked.defaultof<_>

    let createUnionCaseFieldDeconstructions (pattern: IFSharpPattern) (fcsUnionCase: FSharpUnionCase)
            (fcsEntityInstance: FcsEntityInstance) =

        fcsUnionCase.Fields
        |> List.ofSeq
        |> List.map (fun field ->
            let fieldType = field.FieldType.Instantiate(fcsEntityInstance.Substitution).MapType(pattern)
            SingleValueDeconstructionComponent(field.Name, [], fieldType) :> IDeconstructionComponent)

    override this.IsAvailable _ = true
    override this.Text = "Deconstruct pattern"

    override this.CreateBulbItems() =
        let pattern = getPattern ()
        if isNull pattern || not (isApplicablePattern pattern) then Seq.empty else

        let fcsType = getPatternFcsType pattern
        let fcsType = getAbbreviatedType fcsType
        if isNull fcsType then Seq.empty else

        if fcsType.IsTupleType then
            if fcsType.IsStructTupleType || fcsType.GenericArguments.Count > 7 then Seq.empty else

            let typeOwner = pattern.As<IDeclaration>().DeclaredElement.As<ITypeOwner>()
            if isNull typeOwner then Seq.empty else

            let declaredType = typeOwner.Type.As<IDeclaredType>()
            if isNull declaredType then Seq.empty else

            let substitution = declaredType.GetSubstitution()
            let components = 
                substitution.Domain
                |> List.ofSeq
                |> List.map (fun typeParameter ->
                    SingleValueDeconstructionComponent(null, [], substitution.[typeParameter]) :> IDeconstructionComponent)

            let deconstruction = DeconstructionFromTuple(pattern, components)
            DeconstructAction(deconstruction).ToContextActionIntentions() :> _
        else
            let fcsEntityInstance = FcsEntityInstance.create fcsType
            if isNotNull fcsEntityInstance && fcsEntityInstance.Entity.IsFSharpUnion then
                let fcsUnionCases = fcsEntityInstance.Entity.UnionCases
                if fcsUnionCases.Count <> 1 then Seq.empty else

                let fcsUnionCase = fcsUnionCases.[0]
                let components = createUnionCaseFieldDeconstructions pattern fcsUnionCase fcsEntityInstance
                
                let deconstruction = DeconstructionFromUnionCase(fcsUnionCase.Name, pattern, components)
                DeconstructAction(deconstruction).ToContextActionIntentions() :> _
            else
                let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(pattern.As<IWildPat>())
                if isNull parametersOwnerPat then Seq.empty else

                let fcsUnionCase = parametersOwnerPat.ReferenceName.Reference.GetFcsSymbol().As<FSharpUnionCase>()
                if isNull fcsUnionCase then Seq.empty else

                let fcsType = parametersOwnerPat.TryGetFcsType()
                if isNull fcsType then Seq.empty else

                let fcsEntityInstance = FcsEntityInstance.create fcsType
                if isNotNull fcsEntityInstance && not fcsEntityInstance.Entity.IsFSharpUnion then Seq.empty else

                let components = createUnionCaseFieldDeconstructions pattern fcsUnionCase fcsEntityInstance
                let deconstruction = DeconstructionFromUnionCaseFields(parametersOwnerPat, components)
                DeconstructAction(deconstruction).ToContextActionIntentions() :> _
