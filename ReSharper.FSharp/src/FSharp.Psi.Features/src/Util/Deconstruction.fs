namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction

open System.Collections.Generic
open FSharp.Compiler.Symbols
open FSharp.Compiler.Tokenization
open JetBrains.Application.Progress
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Plugins.FSharp.Psi
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


[<AllowNullLiteral>]
type IFSharpDeconstruction =
    inherit IDeconstruction

    abstract Pattern: IFSharpPattern
    abstract Text: string

    abstract DeconstructInnerPatterns: pat: IFSharpPattern * usedNames: ISet<string> -> IFSharpPattern * IFSharpPattern * string list list

module FSharpDeconstruction =
    let getPatternFcsType (pat: IFSharpPattern) =
        match pat with
        | :? IWildPat as wildPat -> wildPat.TryGetFcsType()
        | :? IReferencePat as refPat ->
            match refPat.GetFcsSymbol() with
            | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.FullType
            | _ -> Unchecked.defaultof<_>
        | _ -> Unchecked.defaultof<_>

    let createUnionCaseFields (pattern: IFSharpPattern) (fcsUnionCase: FSharpUnionCase)
            (fcsEntityInstance: FcsEntityInstance) =
        fcsUnionCase.Fields
        |> List.ofSeq
        |> List.map (fun field ->
            let fieldType = field.FieldType.Instantiate(fcsEntityInstance.Substitution).MapType(pattern)
            SingleValueDeconstructionComponent(field.Name, [], fieldType) :> IDeconstructionComponent)

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
        let patternText = names |> Seq.map Seq.head |> String.concat ", "
        let patternText = if isStruct then $"struct ({patternText})" else patternText
        let pattern = factory.CreatePattern(patternText, isTopLevel)

        pattern, names

    let deconstruct moveCaretToEnd (deconstruction: IFSharpDeconstruction) =
        let pat = deconstruction.Pattern

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

        let patternUsedNames = FSharpNamingService.getPatternContextUsedNames pat
        usedNames.AddRange(patternUsedNames)

        if isFromParameter && isNotNull binding then
            for parameterPattern in binding.ParameterPatterns do
                if parameterPattern.Contains(pat) then () else

                parameterPattern.NestedPatterns
                |> Seq.iter (function
                    | :? INamedPat as namedPat -> usedNames.Add(namedPat.SourceName) |> ignore
                    | _ -> ())

        let hasUsages =
            match pat, hasUsages pat with
            | :? IReferencePat as refPat, true ->
                usedNames.Add(refPat.SourceName) |> ignore
                true

            | :? IReferencePat as refPat, false ->
                usedNames.Remove(refPat.SourceName) |> ignore
                false

            | _ -> false

        let pat =
            if hasUsages then
                let refPat = pat :?> IReferencePat
                let asPat = factory.CreatePattern($"_ as {refPat.SourceName}", isTopLevel) :?> IAsPat
                let replacedAsPat = ModificationUtil.ReplaceChild(pat, asPat)
                let replacedAsPat = ParenPatUtil.addParensIfNeeded replacedAsPat
                replacedAsPat.IgnoreInnerParens().As<IAsPat>().Pattern
            else
                pat

        let pat, tuplePattern, names = deconstruction.DeconstructInnerPatterns(pat, usedNames)
        let pattern = ParenPatUtil.addParensIfNeeded tuplePattern

        let itemPatterns: seq<IFSharpPattern> =
            match pattern with
            | null -> Seq.empty
            | :? IAsPat as asPat -> asPat.Pattern.As<ITuplePat>().PatternsEnumerable :> _
            | :? ITuplePat as tuplePat -> tuplePat.PatternsEnumerable :> _
            | :? IReferencePat as refPat -> Seq.singleton refPat |> Seq.cast
            | _ -> invalidOp $"Unexpected pattern: {pattern}"

        if Seq.isEmpty itemPatterns then null else

        (names, itemPatterns) ||> Seq.iter2 (fun names itemPattern ->
            let nameSuggestionsExpression = NameSuggestionsExpression(names)
            let rangeMarker = itemPattern.GetDocumentRange().CreateRangeMarker()
            hotspotsRegistry.Register(rangeMarker, nameSuggestionsExpression))

        let endOffset =
            if moveCaretToEnd then
                let pat: IFSharpPattern = if isNotNull pat then pat else tuplePattern
                pat.GetDocumentEndOffset()
            else
                DocumentOffset.InvalidOffset

        BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, endOffset)

[<AbstractClass>]
type FSharpDeconstructionBase(pattern: IFSharpPattern, components: IDeconstructionComponent list) =
    member val Components = components

    abstract Text: string
    abstract DeconstructInnerPatterns: pat: IFSharpPattern * usedNames: ISet<string> -> IFSharpPattern * IFSharpPattern * string list list

    interface IFSharpDeconstruction with
        member this.Pattern = pattern
        member this.Components = components :> _
        member this.Text = this.Text
        member this.Type = TypeFactory.CreateUnknownType(pattern.GetPsiModule()) :> _
        member this.DeconstructInnerPatterns(pat, usedNames) = this.DeconstructInnerPatterns(pat, usedNames)


type DeconstructionFromTuple(pattern: IFSharpPattern, components: IDeconstructionComponent list, isStruct: bool) =
    inherit FSharpDeconstructionBase(pattern, components)

    member val IsStruct = isStruct

    override this.Text = "Deconstruct tuple"

    static member TryCreate(pattern: IFSharpPattern, fcsType: FSharpType): IFSharpDeconstruction =
        if not fcsType.IsTupleType || fcsType.GenericArguments.Count > 7 then null else

        let typeOwner = pattern.As<IDeclaration>().DeclaredElement.As<ITypeOwner>()
        if isNull typeOwner then null else

        let declaredType = typeOwner.Type.As<IDeclaredType>()
        if isNull declaredType then null else

        let substitution = declaredType.GetSubstitution()
        let components = 
            substitution.Domain
            |> List.ofSeq
            |> List.map (fun typeParameter ->
                SingleValueDeconstructionComponent(null, [], substitution.[typeParameter]) :> IDeconstructionComponent)

        DeconstructionFromTuple(pattern, components, fcsType.IsStructTupleType) :> _

    override this.DeconstructInnerPatterns(pat, usedNames) =
        let pattern, names = FSharpDeconstruction.createInnerPattern pat this isStruct usedNames
        let pattern = ModificationUtil.ReplaceChild(pat, pattern)
        null, pattern, names

type DeconstructionFromUnionCaseFields(name: string, pattern: IParametersOwnerPat, components: IDeconstructionComponent list) =
    inherit FSharpDeconstructionBase(pattern, components)

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

        let components = FSharpDeconstruction.createUnionCaseFields pattern fcsUnionCase fcsEntityInstance
        DeconstructionFromUnionCaseFields(fcsUnionCase.Name, parametersOwnerPat, components) :> _

    override this.DeconstructInnerPatterns(pat, usedNames) =
        let pat = pat :?> IParametersOwnerPat
        let pattern, names = FSharpDeconstruction.createInnerPattern pat this false usedNames
        pat :> _, ModificationUtil.ReplaceChild(pat.Parameters.[0], pattern), names


type DeconstructionFromUnionCase(fcsUnionCase: FSharpUnionCase, pattern: IFSharpPattern,
        components: IDeconstructionComponent list, fcsEntity: FSharpEntity) =
    inherit FSharpDeconstructionBase(pattern, components)

    member val Name = fcsUnionCase.Name
    member val Entity = fcsEntity
    member val UnionCase = fcsUnionCase

    override this.Text = $"Deconstruct '{this.Name}' union case"

    static member Create(pattern, fcsUnionCase: FSharpUnionCase, fcsEntityInstance: FcsEntityInstance) =
        let components = FSharpDeconstruction.createUnionCaseFields pattern fcsUnionCase fcsEntityInstance
        DeconstructionFromUnionCase(fcsUnionCase, pattern, components, fcsEntityInstance.Entity) :> IFSharpDeconstruction

    static member TryCreateFromSingleCaseUnionType(pattern, fcsType): IFSharpDeconstruction =
        let fcsEntityInstance = FcsEntityInstance.create fcsType
        if isNull fcsEntityInstance || not fcsEntityInstance.Entity.IsFSharpUnion then null else

        let fcsUnionCases = fcsEntityInstance.Entity.UnionCases
        if fcsUnionCases.Count <> 1 then null else

        let fcsUnionCase = fcsUnionCases.[0]
        let components = FSharpDeconstruction.createUnionCaseFields pattern fcsUnionCase fcsEntityInstance
        if components.IsEmpty then null else

        DeconstructionFromUnionCase(fcsUnionCase, pattern, components, fcsEntityInstance.Entity) :> _

    override this.DeconstructInnerPatterns(pat, usedNames) =
        let hasFields = fcsUnionCase.HasFields

        let pattern, names =
            if hasFields then
                FSharpDeconstruction.createInnerPattern pat this false usedNames
            else
                null, []

        let name = FSharpKeywords.AddBackticksToIdentifierIfNeeded this.Name
        let name, qualifierTypeElement =
            let typeElement = fcsEntity.GetTypeElement(pat.GetPsiModule())
            let requiresQualifiedName = isNotNull typeElement && typeElement.RequiresQualifiedAccess()
            if requiresQualifiedName then $"{typeElement.GetSourceName()}.{name}", typeElement else

            let containingType = typeElement.GetContainingType()
            if isNotNull containingType && containingType.RequiresQualifiedAccess() then
                $"{containingType.GetSourceName()}.{name}", containingType else

            name, typeElement

        let pat, reference =
            let parametersOwnerPat =
                let factory = pat.CreateElementFactory()
                let text = if hasFields then $"({name} _)" else $"({name})"
                let pattern = factory.CreatePattern(text, false) :?> IParenPat
                pattern.Pattern

            let pat = ModificationUtil.ReplaceChild(pat, parametersOwnerPat)

            let referenceName = 
                match pat with
                | :? IReferencePat as refPat -> refPat.ReferenceName
                | :? IParametersOwnerPat as pa -> pa.ReferenceName
                | _ -> failwith "Unexpected pattern"

            pat, referenceName.Reference

        let unionCase = fcsUnionCase
        if not (FSharpResolveUtil.resolvesToFcsSymbol unionCase reference true "Deconstruct union case") then
            let qualifierReference = reference.QualifierReference
            if isNotNull qualifierReference then
                FSharpReferenceBindingUtil.SetRequiredQualifiers(qualifierReference, qualifierTypeElement)

            let containingModules = getContainingModules pat
            let moduleToOpen = getModuleToOpen qualifierTypeElement
            if not (containingModules.Contains(moduleToOpen)) then
                addOpens reference qualifierTypeElement |> ignore

        if not hasFields then pat, null, [] else

        let pat = ParenPatUtil.addParensIfNeeded pat
        let parametersOwnerPat = pat :?> IParametersOwnerPat
        parametersOwnerPat :> _, ModificationUtil.ReplaceChild(parametersOwnerPat.Parameters.[0], pattern), names


type DeconstructAction(deconstruction: IFSharpDeconstruction) =
    inherit BulbActionBase()

    override this.Text = deconstruction.Text

    override this.ExecutePsiTransaction(_, _) =
        FSharpDeconstruction.deconstruct false deconstruction
