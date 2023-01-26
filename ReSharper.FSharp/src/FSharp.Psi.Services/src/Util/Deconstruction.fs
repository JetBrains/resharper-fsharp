namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Progress
open JetBrains.DocumentModel
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Search
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util.Deconstruction
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText
open JetBrains.Util

type SingleValueDeconstructionComponent(name: string, valueType: IType) =
    member val Name = name
    interface IDeconstructionComponent with
        member this.Type = valueType


[<AllowNullLiteral>]
type IFSharpDeconstruction =
    inherit IDeconstruction

    abstract Text: string

    abstract DeconstructInnerPatterns: pat: IFSharpPattern * usedNames: ISet<string> -> IFSharpPattern * IFSharpPattern * string list list

module FSharpDeconstructionImpl =
    let createUnionCaseFields (context: ITreeNode) (fcsUnionCase: FSharpUnionCase)
            (fcsEntityInstance: FcsEntityInstance) =
        fcsUnionCase.Fields
        |> List.ofSeq
        |> List.map (fun field ->
            let fieldType = field.FieldType.Instantiate(fcsEntityInstance.Substitution).MapType(context)
            SingleValueDeconstructionComponent(field.Name, fieldType) :> IDeconstructionComponent)

    let hasUsages (pat: IFSharpPattern) =
        match pat with
        | :? IReferencePat as refPat ->
            if pat.GetPartialDeclarations() |> Seq.length > 1 then true else

            let element = refPat.DeclaredElement
            let references = List()
            let searchPattern = SearchPattern.FIND_USAGES ||| SearchPattern.FIND_RELATED_ELEMENTS
            let searchDomain = element.GetSearchDomain()
            pat.GetPsiServices().AsyncFinder.Find([| element |], searchDomain, references.ConsumeReferences(),
                searchPattern, NullProgressIndicator.Create())
            references.Any()
        | _ -> false

    let createInnerPattern (pat: IFSharpPattern) (deconstruction: IFSharpDeconstruction) isStruct usedNames =
        let binding, _ = pat.GetBinding(true)
        let factory = pat.CreateElementFactory()

        let components = deconstruction.Components |> List.ofSeq
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
                    | :? SingleValueDeconstructionComponent ->
                        (namesCollection, []) ||> List.fold (fun _ name ->
                            FSharpNamingService.addNames name pat namesCollection)
                    | _ -> namesCollection)
                |> FSharpNamingService.prepareNamesCollection usedNames pat
                |> fun names ->
                    let names = List.ofSeq names @ [if isSingle then "item" else $"item{i + 1}"; "_"]
                    usedNames.Add(names.Head.RemoveBackticks()) |> ignore
                    List.distinct names)

        let isTopLevel = binding :? ITopBinding
        let patternText = names |> Seq.map Seq.head |> String.concat ", "
        let patternText = if isStruct then $"struct ({patternText})" else patternText
        let pattern = factory.CreatePattern(patternText, isTopLevel)

        pattern, names

    let deconstructImpl ignoreUsages (deconstruction: IFSharpDeconstruction) (pat: IFSharpPattern) =
        use writeCookie = WriteLockCookie.Create(pat.IsPhysical())
        let factory = pat.CreateElementFactory()
        let hotspotsRegistry = HotspotsRegistry(pat.GetPsiServices())

        let binding, isFromParameter = pat.GetBinding(true)
        let binding = binding.As<IBinding>()
        let isTopLevel = binding :? ITopBinding

        let inExprs =
            let letExpr = LetBindingsNavigator.GetByBinding(binding.As()).As<ILetOrUseExpr>()
            if not isFromParameter && isNotNull letExpr then [letExpr.InExpression] else

            if isFromParameter && isNotNull binding then [binding.Expression] else

            let pat = skipIntermediatePatParents pat

            let matchClause = MatchClauseNavigator.GetByPattern(pat)
            if isNotNull matchClause then [matchClause.WhenExpression; matchClause.Expression] else

            let lambdaExpr = LambdaExprNavigator.GetByPattern(pat)
            if isNotNull lambdaExpr then [lambdaExpr.Expression] else

            let memberDeclaration = MemberDeclarationNavigator.GetByParameterPattern(pat)
            if isNotNull memberDeclaration then [memberDeclaration.Expression] else

            let accessorDeclaration = AccessorDeclarationNavigator.GetByParameterPattern(pat)
            if isNotNull accessorDeclaration then [accessorDeclaration.Expression] else

            []

        let containingType = FSharpNamingService.getPatternContainingType pat
        let usedNames = FSharpNamingService.getUsedNames inExprs EmptyList.InstanceList containingType true

        let patternUsedNames =
            FSharpNamingService.getPatternContextUsedNames pat
            |> Seq.map (fun name -> name.RemoveBackticks())
        usedNames.AddRange(patternUsedNames)

        if isFromParameter && isNotNull binding then
            for parameterPattern in binding.ParameterPatterns do
                if parameterPattern.Contains(pat) then () else

                parameterPattern.NestedPatterns
                |> Seq.iter (function
                    | :? IReferencePat as refPat -> usedNames.Add(refPat.SourceName.RemoveBackticks()) |> ignore
                    | _ -> ())

        let hasUsages =
            let refPat = pat.As<IReferencePat>()
            if isNull refPat then false else

            if not ignoreUsages && hasUsages pat then
                usedNames.Add(refPat.SourceName.RemoveBackticks()) |> ignore
                true
            else
                usedNames.Remove(refPat.SourceName.RemoveBackticks()) |> ignore
                false

        let pat =
            if hasUsages then
                let refPat = pat :?> IReferencePat
                let asPat = factory.CreatePattern($"_ as {refPat.SourceName}", isTopLevel) :?> IAsPat
                let replacedAsPat = ModificationUtil.ReplaceChild(pat, asPat)
                let replacedAsPat = ParenPatUtil.addParensIfNeeded replacedAsPat
                replacedAsPat.IgnoreInnerParens().As<IAsPat>().LeftPattern
            else
                pat

        let pat, tuplePattern, names = deconstruction.DeconstructInnerPatterns(pat, usedNames)
        let pattern = ParenPatUtil.addParensIfNeeded tuplePattern

        let parenPat = ParenPatNavigator.GetByPattern(pattern)
        let patternDeclaration = ParametersPatternDeclarationNavigator.GetByPattern(parenPat)
        let pattern =
            if isNull pat && isNotNull patternDeclaration then
                ParenPatUtil.addParens pattern
            else
                pattern

        let itemPatterns: seq<IFSharpPattern> =
            match pattern with
            | null -> Seq.empty
            | :? IAsPat as asPat -> asPat.LeftPattern.As<ITuplePat>().PatternsEnumerable :> _
            | :? ITuplePat as tuplePat -> tuplePat.PatternsEnumerable :> _
            | :? IReferencePat as refPat -> Seq.singleton refPat |> Seq.cast
            | _ -> invalidOp $"Unexpected pattern: {pattern}"

        if Seq.isEmpty itemPatterns then None else

        (names, itemPatterns) ||> Seq.iter2 (fun names itemPattern ->
            let nameSuggestionsExpression = NameSuggestionsExpression(names)
            let rangeMarker = itemPattern.GetDocumentRange().CreateRangeMarker()
            hotspotsRegistry.Register(rangeMarker, nameSuggestionsExpression))

        let pat: IFSharpPattern = if isNotNull pat then pat else tuplePattern
        Some (hotspotsRegistry, pat)


[<AbstractClass; AllowNullLiteral>]
type FSharpDeconstructionBase(components: IDeconstructionComponent list) =
    member val Components = components

    abstract Text: string
    abstract DeconstructInnerPatterns: pat: IFSharpPattern * usedNames: ISet<string> -> IFSharpPattern * IFSharpPattern * string list list

    interface IFSharpDeconstruction with
        member this.Components = components :> _
        member this.Text = this.Text
        member this.Type = failwith "todo"

        member this.DeconstructInnerPatterns(pat, usedNames) =
            this.DeconstructInnerPatterns(pat, usedNames)


type DeconstructionFromTuple(components: IDeconstructionComponent list, isStruct: bool) =
    inherit FSharpDeconstructionBase(components)

    member val IsStruct = isStruct

    override this.Text = "Deconstruct tuple"

    static member TryCreate(context: ITreeNode, fcsType: FSharpType): IFSharpDeconstruction =
        if not fcsType.IsTupleType || fcsType.GenericArguments.Count > 7 then null else

        let declaredType = fcsType.MapType(context).As<IDeclaredType>()
        let substitution = declaredType.GetSubstitution()
        let components = 
            substitution.Domain
            |> List.ofSeq
            |> List.map (fun typeParameter ->
                SingleValueDeconstructionComponent(null, substitution[typeParameter]) :> IDeconstructionComponent)

        DeconstructionFromTuple(components, fcsType.IsStructTupleType) :> _

    override this.DeconstructInnerPatterns(pat, usedNames) =
        let pattern, names = FSharpDeconstructionImpl.createInnerPattern pat this isStruct usedNames
        let pattern = ModificationUtil.ReplaceChild(pat, pattern)
        null, pattern, names

[<AllowNullLiteral>]
type DeconstructionFromUnionCaseFields(name: string, components: IDeconstructionComponent list) =
    inherit FSharpDeconstructionBase(components)

    member val Name = name
    override this.Text = $"Deconstruct '{name}' fields"

    static member TryCreate(pattern: IFSharpPattern, acceptRefPatWildPatOnly): IFSharpDeconstruction =
        let pattern = if not acceptRefPatWildPatOnly then pattern else pattern.As<IWildPat>() :> _
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(pattern.IgnoreParentParens())
        if isNull parametersOwnerPat then null else

        let fcsUnionCase = parametersOwnerPat.ReferenceName.Reference.GetFcsSymbol().As<FSharpUnionCase>()
        if isNull fcsUnionCase then null else

        let fcsType = parametersOwnerPat.TryGetFcsType()
        if isNull fcsType then null else

        let fcsEntityInstance = FcsEntityInstance.create fcsType
        if isNotNull fcsEntityInstance && not fcsEntityInstance.Entity.IsFSharpUnion then null else

        let components = FSharpDeconstructionImpl.createUnionCaseFields pattern fcsUnionCase fcsEntityInstance
        DeconstructionFromUnionCaseFields(fcsUnionCase.Name, components) :> _

    override this.DeconstructInnerPatterns(pat, usedNames) =
        let pat = ParametersOwnerPatNavigator.GetByParameter(pat.IgnoreParentParens()).NotNull()
        let pattern, names = FSharpDeconstructionImpl.createInnerPattern pat this false usedNames
        pat :> _, ModificationUtil.ReplaceChild(pat.Parameters[0], pattern), names


type DeconstructionFromUnionCase(fcsUnionCase: FSharpUnionCase,
        components: IDeconstructionComponent list, fcsEntity: FSharpEntity) =
    inherit FSharpDeconstructionBase(components)

    let [<Literal>] opName = "DeconstructionFromUnionCase.DeconstructInnerPatterns"

    member val Name = fcsUnionCase.Name
    member val Entity = fcsEntity
    member val UnionCase = fcsUnionCase

    override this.Text = $"Deconstruct '{this.Name}' union case"

    static member Create(pattern, fcsUnionCase: FSharpUnionCase, fcsEntityInstance: FcsEntityInstance) =
        let components = FSharpDeconstructionImpl.createUnionCaseFields pattern fcsUnionCase fcsEntityInstance
        DeconstructionFromUnionCase(fcsUnionCase, components, fcsEntityInstance.Entity) :> IFSharpDeconstruction

    static member TryCreateFromSingleCaseUnionType(context: ITreeNode, fcsType): IFSharpDeconstruction =
        let fcsEntityInstance = FcsEntityInstance.create fcsType
        if isNull fcsEntityInstance || not fcsEntityInstance.Entity.IsFSharpUnion then null else

        let fcsUnionCases = fcsEntityInstance.Entity.UnionCases
        if fcsUnionCases.Count <> 1 then null else

        let fcsUnionCase = fcsUnionCases[0]
        let components = FSharpDeconstructionImpl.createUnionCaseFields context fcsUnionCase fcsEntityInstance
        if components.IsEmpty then null else

        DeconstructionFromUnionCase(fcsUnionCase, components, fcsEntityInstance.Entity) :> _

    override this.DeconstructInnerPatterns(pat, usedNames) =
        let hasFields = fcsUnionCase.HasFields

        let pattern, names =
            if hasFields then
                FSharpDeconstructionImpl.createInnerPattern pat this false usedNames
            else
                null, []

        let pat = FSharpPatternUtil.bindFcsSymbol pat fcsUnionCase opName
        if not hasFields then pat, null, [] else

        let parametersOwnerPat = FSharpPatternUtil.toParameterOwnerPat pat opName
        let pat = ParenPatUtil.addParensIfNeeded parametersOwnerPat
        let parametersOwnerPat = pat.IgnoreInnerParens() :?> IParametersOwnerPat
        parametersOwnerPat :> _, ModificationUtil.ReplaceChild(parametersOwnerPat.Parameters[0], pattern), names


[<AllowNullLiteral>]
type DeconstructionFromKeyValuePair(components: IDeconstructionComponent list) =
    inherit FSharpDeconstructionBase(components)

    static let keyValuePairTypeName = FSharpPredefinedType.clrTypeName "System.Collections.Generic.KeyValuePair`2"

    let [<Literal>] opName = "DeconstructionFromUnionCase.DeconstructInnerPatterns"

    override this.Text = $"Deconstruct 'KeyValuePair'"

    static member TryCreate(context: ITreeNode, fcsType: FSharpType): IFSharpDeconstruction =
        let declaredType = fcsType.MapType(context).As<IDeclaredType>()
        if isNull declaredType then null else

        let typeElement = declaredType.GetTypeElement()
        if isNull typeElement then null else

        if not (typeElement.GetClrName().Equals(keyValuePairTypeName)) then null else

        let substitution = declaredType.GetSubstitution()
        let components = 
            substitution.Domain
            |> List.ofSeq
            |> List.map (fun typeParameter ->
                SingleValueDeconstructionComponent(null, substitution[typeParameter]) :> IDeconstructionComponent)

        DeconstructionFromKeyValuePair(components) :> _

    override this.DeconstructInnerPatterns(pat, usedNames) =
        let pat = ModificationUtil.ReplaceChild(pat, pat.CreateElementFactory().CreatePattern("KeyValue", false))
        let pat = FSharpPatternUtil.toParameterOwnerPat pat opName

        let pat = ParenPatUtil.addParensIfNeeded pat
        let parametersOwnerPat = pat.IgnoreInnerParens() :?> IParametersOwnerPat

        let pattern, names = FSharpDeconstructionImpl.createInnerPattern pat this false usedNames
        parametersOwnerPat :> _, ModificationUtil.ReplaceChild(parametersOwnerPat.Parameters[0], pattern), names


module FSharpDeconstruction =
    let getPatternFcsType (pat: IFSharpPattern) =
        match pat with
        | :? IWildPat as wildPat -> wildPat.TryGetFcsType()
        | :? IReferencePat as refPat ->
            match refPat.GetFcsSymbol() with
            | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.FullType
            | _ -> Unchecked.defaultof<_>
        | _ -> Unchecked.defaultof<_>

    let deconstruct ignoreUsages (endOffsetNode: ITreeNode) deconstruction (pattern: IFSharpPattern) =
        match FSharpDeconstructionImpl.deconstructImpl ignoreUsages deconstruction pattern with
        | Some(hotspotsRegistry, _) ->
            let offset =
                if isNotNull endOffsetNode then endOffsetNode.GetDocumentEndOffset() else DocumentOffset.InvalidOffset
            BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, offset)
        | _ -> null

    let tryGetDeconstruction (context: ITreeNode) (fcsType: FSharpType) =
        [ DeconstructionFromTuple.TryCreate(context, fcsType)
          DeconstructionFromUnionCase.TryCreateFromSingleCaseUnionType(context, fcsType)
          DeconstructionFromKeyValuePair.TryCreate(context, fcsType) ]
          |> List.tryFind isNotNull


type DeconstructAction(pat: IFSharpPattern, deconstruction: IFSharpDeconstruction) =
    inherit BulbActionBase()

    let mutable ignoreUsages = false

    override this.Text = deconstruction.Text

    override this.Execute(solution, textControl) =
        let refPat = pat.As<IReferencePat>()
        if isNull refPat then base.Execute(solution, textControl) else

        let hasUsages = FSharpDeconstructionImpl.hasUsages pat
        if not hasUsages then base.Execute(solution, textControl) else

        let keepPatternText =
            let richText = RichText("Add '")
            richText.Append($"as {refPat.SourceName}", TextStyle(JetFontStyles.Bold)) |> ignore
            richText.Append("' pattern", TextStyle()) |> ignore
            richText

        let occurrences =
            [|WorkflowPopupMenuOccurrence(keepPatternText, RichText.Empty, false)
              WorkflowPopupMenuOccurrence(RichText("Remove pattern"), RichText.Empty, true)|]

        let popupMenu = solution.GetComponent<WorkflowPopupMenu>()
        let occurrence =
            popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)

        if isNull occurrence then () else

        occurrence
        |> Option.ofObj
        |> Option.bind (fun occurrence -> occurrence.Entities |> Seq.tryHead)
        |> Option.iter (fun keepPatternOccurrence -> ignoreUsages <- keepPatternOccurrence)

        base.Execute(solution, textControl)

    override this.ExecutePsiTransaction(_, _) =
        FSharpDeconstruction.deconstruct ignoreUsages null deconstruction pat
