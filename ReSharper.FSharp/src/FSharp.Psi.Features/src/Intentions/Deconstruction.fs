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
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Search
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util.Deconstruction
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type SingleValueDeconstructionComponent(valueType: IType) =
    interface IDeconstructionComponent with
        member this.Type = valueType

type DeconstructionFromTuple(pattern: IFSharpPattern, components: IDeconstructionComponent list) =
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

    override this.ExecutePsiTransaction(_, _) =
        match deconstruction with
        | :? DeconstructionFromTuple as tupleDeconstruction ->
            let pat = tupleDeconstruction.Pattern
            let binding = pat.GetBinding()

            let inExpr =
                match LetBindingsNavigator.GetByBinding(binding.As()) with
                | :? ILetOrUseExpr as letExpr -> letExpr.InExpression
                | _ -> null

            let containingType = FSharpNamingService.getPatternContainingType pat
            let usedNames = FSharpNamingService.getUsedNames inExpr EmptyList.InstanceList containingType false

            let hasUsages = 
                match pat, hasUsages pat with
                | :? IReferencePat as refPat, true ->
                    usedNames.Add(refPat.SourceName) |> ignore
                    true

                | :? IReferencePat as refPat, false ->
                    usedNames.Remove(refPat.SourceName) |> ignore
                    false

                | _ -> false

            let names = 
                tupleDeconstruction.Components
                |> List.mapi (fun i tupleComponent ->
                    FSharpNamingService.createEmptyNamesCollection pat
                    |> FSharpNamingService.addNamesForType tupleComponent.Type
                    |> FSharpNamingService.prepareNamesCollection usedNames pat
                    |> fun names ->
                        let names = List.ofSeq names @ [$"item{i + 1}"; "_"]
                        usedNames.Add(names.Head) |> ignore
                        names)

            let factory = pat.CreateElementFactory()
            let pattern =
                let isTopLevel = binding :? ITopBinding
                let patternText = names |> List.map List.head |> String.concat ", "

                let tuplePattern = factory.CreatePattern(patternText, isTopLevel) :?> ITuplePat

                if hasUsages then
                    let refPat = pat :?> IReferencePat
                    let asPat = factory.CreatePattern($"_ as {refPat.SourceName}", isTopLevel) :?> IAsPat
                    asPat.SetPattern(tuplePattern) |> ignore
                    asPat :> IFSharpPattern
                else
                    tuplePattern :> _

            use writeCookie = WriteLockCookie.Create(pat.IsPhysical())

            let hotspotsRegistry = HotspotsRegistry(pat.GetPsiServices())
            let pattern = ModificationUtil.ReplaceChild(pat, pattern)

            let pattern: IFSharpPattern =
                let contextPattern = pattern.IgnoreParentParens()
                if contextPattern == pattern && RedundantParenPatAnalyzer.needsParens contextPattern pattern then
                    let parenPattern = factory.CreatePattern("(_)", false) :?> IParenPat
                    let patternCopy = pattern.Copy()
                    let parenPattern = ModificationUtil.ReplaceChild(pattern, parenPattern)
                    parenPattern.SetPattern(patternCopy)
                else
                    pattern

            let tuplePatterns =
                match pattern with
                | :? IAsPat as asPat -> asPat.Pattern.As<ITuplePat>().Patterns
                | :? ITuplePat as tuplePat -> tuplePat.Patterns
                | _ -> invalidOp "Unexpected pattern"

            (names, tuplePatterns) ||> Seq.iter2 (fun names p ->
                let nameSuggestionsExpression = NameSuggestionsExpression(names)
                let rangeMarker = p.GetDocumentRange().CreateRangeMarker()
                hotspotsRegistry.Register(rangeMarker, nameSuggestionsExpression))

            BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, DocumentOffset.InvalidOffset)

        | _ -> null

    override this.Text = "Deconstruct tuple"


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

    let getPatternType (pat: IFSharpPattern) =
        match pat with
        | :? IWildPat as wildPat -> wildPat.TryGetFcsType()
        | :? IReferencePat as refPat ->
            match refPat.GetFSharpSymbol() with
            | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.FullType
            | _ -> Unchecked.defaultof<_>
        | _ -> Unchecked.defaultof<_>

    override this.IsAvailable _ = true
    override this.Text = "Deconstruct pattern"

    override this.CreateBulbItems() =
        let pattern = getPattern ()
        if isNull pattern || not (isApplicablePattern pattern) then Seq.empty else

        let fcsType = getPatternType pattern
        if isNull fcsType || not fcsType.IsTupleType || fcsType.IsStructTupleType ||
                fcsType.GenericArguments.Count > 7 then Seq.empty else

        let typeOwner = pattern.As<IDeclaration>().DeclaredElement.As<ITypeOwner>()
        if isNull typeOwner then Seq.empty else

        let declaredType = typeOwner.Type.As<IDeclaredType>()
        if isNull declaredType then Seq.empty else

        let substitution = declaredType.GetSubstitution()
        let components = 
            substitution.Domain
            |> List.ofSeq
            |> List.map (fun typeParameter ->
                SingleValueDeconstructionComponent(substitution.[typeParameter]) :> IDeconstructionComponent)

        let deconstruction = DeconstructionFromTuple(pattern, components)
        DeconstructAction(deconstruction).ToContextActionIntentions() :> _
