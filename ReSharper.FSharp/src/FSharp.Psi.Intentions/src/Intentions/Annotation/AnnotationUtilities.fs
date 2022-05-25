module JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions

open FSharp.Compiler.Symbols
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

module Declaration =
    [<return: Struct>]
    let (|IsNotNullAndHasMfvSymbolUse|_|) (declaration: IFSharpDeclaration) =
        if isNull declaration then ValueNone else
        let symbolUse = declaration.GetFcsSymbolUse()
        if isNull symbolUse then ValueNone else

        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            ValueSome struct (symbolUse, mfv)
        | _ ->
            ValueNone

module PatUtil =

    let getReturnTypeInfo (pattern: IFSharpPattern) =
        let binding = BindingNavigator.GetByHeadPattern(pattern)
        if isNotNull binding && isNotNull binding.ReturnTypeInfo then
            ValueSome binding.ReturnTypeInfo
        else
            ValueNone

    let removeTypeAnnotations (pattern: IFSharpPattern) =
        match pattern with
        | :? ITypedPat as typedPat ->
            ModificationUtil.ReplaceChild(typedPat, typedPat.Pattern.IgnoreInnerParens())
        | _ ->
            match getReturnTypeInfo pattern with
            | ValueSome typeInfo ->
                ModificationUtil.DeleteChild(typeInfo.Colon)
                ModificationUtil.DeleteChild(typeInfo)
            | _ ->
                ()

            pattern

    let removeInnerParens (pattern: IFSharpPattern) =
        let updatedPattern = pattern.IgnoreInnerParens()
        if pattern == updatedPattern then
            pattern
        else
            ModificationUtil.ReplaceChild(pattern, updatedPattern)

module AnnotationUtil =

    let private isFullyAnnotatedNamedTypeUsage (namedTypeUsage: INamedTypeUsage) =
        isNotNull namedTypeUsage.ReferenceName &&
        (isNull namedTypeUsage.ReferenceName.TypeArgumentList ||
        namedTypeUsage.ReferenceName.TypeArgumentList.TypeUsagesEnumerable
           |> Seq.exists (fun typeUsage -> typeUsage :? IAnonTypeUsage)
           |> not)

    let rec private isFullyAnnotatedFunctionTypeUsage (functionTypeUsage: IFunctionTypeUsage) =
        isFullyAnnotatedTypeUsage functionTypeUsage.ArgumentTypeUsage &&
        isFullyAnnotatedTypeUsage functionTypeUsage.ReturnTypeUsage

    and isFullyAnnotatedTypeUsage (typeUsage: ITypeUsage) =
        match typeUsage with
        | :? INamedTypeUsage as namedTypeUsage ->
            isFullyAnnotatedNamedTypeUsage namedTypeUsage
        | :? IFunctionTypeUsage as functionTypeUsage ->
            isFullyAnnotatedFunctionTypeUsage functionTypeUsage
        | :? IConstrainedTypeUsage as constrainedTypeUsage ->
            isFullyAnnotatedTypeUsage constrainedTypeUsage.TypeUsage
        | _ ->
            false

    let isFullyAnnotatedReturnTypeInfo (returnTypeInfo: IReturnTypeInfo) =
        isNotNull returnTypeInfo &&
        isFullyAnnotatedTypeUsage returnTypeInfo.ReturnType

    let rec isFullyAnnotatedPattern (pattern: IFSharpPattern) =
        match pattern.IgnoreInnerParens() with
        | :? IUnitPat ->
            true
        | :? ITypedPat as typedPat ->
            typedPat.TypeUsage
            |> isFullyAnnotatedTypeUsage
        | pattern ->
            pattern
            |> PatUtil.getReturnTypeInfo
            |> ValueOption.exists isFullyAnnotatedReturnTypeInfo

    let isFullyAnnotatedBinding (binding: IBinding) =
        isFullyAnnotatedReturnTypeInfo binding.ReturnTypeInfo &&
        binding.ParameterPatternsEnumerable |> Seq.forall isFullyAnnotatedPattern

    let isFullyAnnotatedMemberDeclaration (memberDeclaration: IMemberDeclaration) =
        isFullyAnnotatedReturnTypeInfo memberDeclaration.ReturnTypeInfo &&
        memberDeclaration.ParameterPatternsEnumerable |> Seq.forall isFullyAnnotatedPattern

    // let f x ... =
    // let f<'a> = ...
    let isFunctionBinding (binding: IBinding) =
        binding.HasParameters ||
        binding.HeadPattern.GetNextMeaningfulSibling() :? IPostfixTypeParameterDeclarationList

    // let x = y
    // let x = fun y -> ...
    let isValueBinding (binding: IBinding) =
       not (isFunctionBinding binding)

module SpecifyUtil =

    let private addSpaceBeforeColon forceSpaceBeforeColon (pattern: ITreeNode) =
        if forceSpaceBeforeColon || pattern.GetSettingsStoreWithEditorConfig().GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(pattern, Whitespace()) |> ignore

    let private addReturnTypeInfo typeString (node: ITreeNode) =
        let factory = node.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString)
        ModificationUtil.AddChildAfter(node, factory.CreateReturnTypeInfo(typeUsage))

    let private replaceWithTypedPattern typeString (pattern: IFSharpPattern) =
        let factory = pattern.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString)
        ModificationUtil.ReplaceChild(pattern, factory.CreateTypedPat(pattern, typeUsage))

    let addParens (pattern: IFSharpPattern) =
        let factory = pattern.CreateElementFactory()
        let parenPat = factory.CreateParenPat()
        parenPat.SetPattern(pattern) |> ignore
        ModificationUtil.ReplaceChild(pattern, parenPat) |> ignore

    let private specifyConstrainedTypeUsage displayContext (fcsType: FSharpType) (typeUsage: IConstrainedTypeUsage) =
        let typeString = fcsType.Format(displayContext)
        let factory = typeUsage.CreateElementFactory()
        typeUsage.SetTypeUsage(factory.CreateTypeUsage(typeString))

    let rec private specifyTuplePat displayContext (fcsType: FSharpType) (pattern: ITuplePat) =
        let innerPatterns = pattern.Patterns
        for i = 0 to innerPatterns.Count - 1 do
            specifyPattern displayContext fcsType.GenericArguments[i] false innerPatterns[i]
        addParens pattern

    and specifyPattern displayContext (fcsType: FSharpType) forceParens (pattern: IFSharpPattern) =

        match pattern.IgnoreInnerParens() with
        | :? ITypedPat as typedPat when (typedPat.TypeUsage :? IConstrainedTypeUsage) ->
            specifyConstrainedTypeUsage displayContext fcsType (typedPat.TypeUsage :?> IConstrainedTypeUsage) |> ignore

        | _ ->
            let pattern =
                pattern
                |> PatUtil.removeInnerParens
                |> PatUtil.removeTypeAnnotations

            let forceParens = forceParens && not (pattern.Parent :? IParenPat)

            match pattern with
            | :? ITuplePat as tuplePat ->
                specifyTuplePat displayContext fcsType tuplePat

            | pattern ->
                let typeString = fcsType.Format(displayContext)
                pattern
                |> replaceWithTypedPattern typeString
                |> fun pattern ->
                    if forceParens then
                        addParens pattern

    let specifyMethodReturnType displayContext (mfv: FSharpMemberOrFunctionOrValue) (method: IMemberDeclaration) =
        let typeString = mfv.ReturnParameter.Type.Format(displayContext)
        let anchor = method.ParametersDeclarationsEnumerable.LastOrDefault()

        anchor
        |> addReturnTypeInfo typeString
        |> addSpaceBeforeColon false

    let specifyFunctionBindingReturnType displayContext (mfv: FSharpMemberOrFunctionOrValue) (binding: IBinding) =
        let parameters = binding.ParametersDeclarationsEnumerable
        let fcsType, anchor =
            if parameters.IsEmpty() then
                // let f<'a>{here} = ...
                let headPatOrGenericParameterDeclaration =
                    match binding.HeadPattern.GetNextMeaningfulSibling() with
                    | :? IPostfixTypeParameterDeclarationList as typeParam ->
                        typeParam :> ITreeNode
                    | _ ->
                        binding.HeadPattern
                mfv.FullType, headPatOrGenericParameterDeclaration
            else
                // let f x{here} = ...
                // this enumerates enumerable 2 times, not sure what can I do about it?
                let fcsType = FcsTypeUtil.getFunctionReturnType (parameters.Count()) mfv.FullType
                fcsType, parameters.LastOrDefault()

        let typeString = fcsType.Format(displayContext)
        let forceSpaceBeforeColon = anchor :? IPostfixTypeParameterDeclarationList

        anchor
        |> addReturnTypeInfo typeString
        |> addSpaceBeforeColon forceSpaceBeforeColon

    let specifyPropertyType displayContext (fcsType: FSharpType) (decl: IMemberDeclaration) =
        Assertion.Assert(decl.ParametersDeclarationsEnumerable.IsEmpty(),
            "decl.ParametersDeclarationsEnumerable.IsEmpty()")

        if isNotNull decl.ReturnTypeInfo then
            ModificationUtil.DeleteChild(decl.ReturnTypeInfo)

        let factory = decl.CreateElementFactory()
        let returnTypeInfo = factory.CreateReturnTypeInfo(factory.CreateTypeUsage(fcsType.Format(displayContext)))
        ModificationUtil.AddChildAfter(decl.Identifier, returnTypeInfo) |> ignore

module StandaloneAnnotationUtil =

    let private specifyTuplePat (tuplePat: ITuplePat) =
        // this is a funky quirk
        // if we specify tuple patterns in sequence manner
        // then after first modification fcs is unable to find the corresponding symbolUse for next patterns
        // so we have to find all ref pats and their types first

        let referenceList = ResizeArray()

        let getTupleChildrenRefPatsReferences (tuplePat: ITuplePat) = [|
            for pattern in tuplePat.PatternsEnumerable do
                if AnnotationUtil.isFullyAnnotatedPattern pattern then () else
                let refPat =
                    match pattern.IgnoreInnerParens() with
                    | :? IReferencePat as localRef ->
                        localRef
                    | :? ITypedPat as typedPat when (typedPat.Pattern :? IReferencePat) ->
                        typedPat.Pattern :?> IReferencePat
                    | _ ->
                        null

                match refPat with
                | Declaration.IsNotNullAndHasMfvSymbolUse(symbolUse, mfv) ->
                    struct (refPat, symbolUse.DisplayContext, mfv.FullType)
                | _ ->
                    ()
        |]

        let rec getRefPats (pattern: ITuplePat) =
            let childrenReferences = getTupleChildrenRefPatsReferences pattern
            referenceList.Add(struct (pattern, childrenReferences))
            for pattern in pattern.PatternsEnumerable do
                match pattern.IgnoreInnerParens() with
                | :? ITuplePat as nestedTuplePat ->
                    getRefPats nestedTuplePat
                | _ ->
                    ()

        getRefPats tuplePat

        // this is yet another quirk:
        // we have to specify patterns in backwards order to keep tree valid
        // otherwise we lose parent property of tuplePat
        for i = referenceList.Count - 1 downto 0 do
            let struct (tuplePat, refPats) = referenceList[i]
            for refPat, displayContext, fcsType in refPats do
                SpecifyUtil.specifyPattern displayContext fcsType false refPat

            match tuplePat.IgnoreParentParens() with
            | :? IParenPat ->
                ()
            | _ ->
                SpecifyUtil.addParens tuplePat

    let rec isSupportedPatternForStandaloneAnnotation (pattern: IFSharpPattern) =
        match pattern.IgnoreInnerParens() with
        | :? ITypedPat as typedPat ->
            typedPat.Pattern.IgnoreInnerParens() :? IReferencePat &&
            not (AnnotationUtil.isFullyAnnotatedPattern typedPat)
        | :? IReferencePat as refPat ->
            not (AnnotationUtil.isFullyAnnotatedPattern refPat)
        | :? ITuplePat as tuplePat ->
            tuplePat.PatternsEnumerable |> Seq.exists isSupportedPatternForStandaloneAnnotation
        | _ ->
            false

    let specifyPatternThatSupportsStandaloneAnnotation forceParens (pattern: IFSharpPattern) =
        match pattern.IgnoreInnerParens() with
        | :? ITypedPat as typedPat ->
            match typedPat.Pattern.IgnoreInnerParens() with
            | :? IReferencePat as Declaration.IsNotNullAndHasMfvSymbolUse(symbolUse, mfv) ->
                SpecifyUtil.specifyPattern symbolUse.DisplayContext mfv.FullType false pattern
            | _ ->
                ()
        | :? IReferencePat as Declaration.IsNotNullAndHasMfvSymbolUse(symbolUse, mfv) ->
                SpecifyUtil.specifyPattern symbolUse.DisplayContext mfv.FullType forceParens pattern

        | :? ITuplePat as tuplePat ->
            specifyTuplePat tuplePat

        | _ ->
            ()
