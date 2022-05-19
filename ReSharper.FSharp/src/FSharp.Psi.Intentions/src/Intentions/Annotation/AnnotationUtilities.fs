module JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

// This file will a be a base for new work

// Value is ether local or parameter
// Values can be: LocalRefs, Patterns, Arrays, Lists, Wilds

// ReferencePat is useful as it have it's own link to unique FSharpSymbol, others do not seem to

// Is there a common local value interface to look for?

// Functions are always bindings so always LetBinding
// Functions can be type functions, normal functions and values

// Members are always members so always MemberDeclaration
// Members can be properties and methods

// TODO's:
// 1. Decide how many different analyzers needs to be.
// Functions/Values/Members/Tuples - is there a need for specific Tuple analyzer?

// 2. Extract shared logic, cleanup

// 3. More tests

// isFullyAnnotatedPattern
// isPartiallyAnnotatedPattern

// isFullyAnnotatedBinding
// isFullyAnnotatedMethod
// isFullyAnnotatedProperty
// isFullyAnnotatedPat ?

module PatUtil2 =

    [<return: Struct>]
    let (|HasMfvSymbolUse|_|) (referencePat: IReferencePat) =
        if isNull referencePat then ValueNone else
        let symbolUse = referencePat.GetFcsSymbolUse()
        if isNotNull symbolUse then ValueNone else

        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            ValueSome (symbolUse, mfv)
        | _ ->
            ValueNone

    let getReturnTypeInfo (referencePat: IReferencePat) =
        match referencePat.GetNextMeaningfulSibling() with
        | :? IReturnTypeInfo as returnTypeInfo ->
            ValueSome returnTypeInfo
        | _ ->
            ValueNone

    let removeReturnTypeInfo (pattern: IFSharpPattern) =
        match pattern.GetNextMeaningfulSibling() with
        | :? IReturnTypeInfo as returnTypeInfo ->
            ModificationUtil.DeleteChild(returnTypeInfo)
        | _ ->
            ()
        pattern

    let removeInnerParens (pattern: IFSharpPattern) =
        let updatedPattern = pattern.IgnoreInnerParens()
        if pattern == updatedPattern then
            pattern
        else
            ModificationUtil.ReplaceChild(pattern, updatedPattern)

    let isPartiallyAnnotatedNamedTypeUsage (namedTypeUsage: INamedTypeUsage) =
        isNotNull namedTypeUsage.ReferenceName &&
        isNotNull namedTypeUsage.ReferenceName.TypeArgumentList &&
        namedTypeUsage.ReferenceName.TypeArgumentList.TypeUsagesEnumerable
           |> Seq.exists (fun typeUsage -> typeUsage :? IAnonTypeUsage)

    let isPartiallyAnnotatedReturnTypeInfo (typedPat: IReturnTypeInfo) =
        match typedPat.ReturnType with
        | :? INamedTypeUsage as namedTypeUsage ->
            isPartiallyAnnotatedNamedTypeUsage namedTypeUsage
        | _ ->
            false

    let isPartiallyAnnotatedTypedPat (typedPat: ITypedPat) =
        match typedPat.TypeUsage with
        | :? INamedTypeUsage as namedTypeUsage ->
            isPartiallyAnnotatedNamedTypeUsage namedTypeUsage

        | :? IFunctionTypeUsage as functionTypeUsage ->
            match functionTypeUsage.ArgumentTypeUsage, functionTypeUsage.ReturnTypeUsage with
            | :? INamedTypeUsage as namedTypeUsage, _
            | _, (:? INamedTypeUsage as namedTypeUsage) ->
                isPartiallyAnnotatedNamedTypeUsage namedTypeUsage
            | _ ->
                false
        | _ ->
            false

    let isPartiallyAnnotatedRefPat (referencePat: IReferencePat) =
        match getReturnTypeInfo referencePat with
        | ValueSome returnTypeInfo ->
            isPartiallyAnnotatedReturnTypeInfo returnTypeInfo
        | ValueNone ->
            true

    let rec isPartiallyAnnotatedTuplePat (tuplePat: ITuplePat) =
        tuplePat.PatternsEnumerable
        |> Seq.exists (fun pattern ->
            match pattern with
            | :? IReferencePat as referencePat ->
                isPartiallyAnnotatedRefPat referencePat
            | :? ITuplePat as tuplePat ->
                isPartiallyAnnotatedTuplePat tuplePat
            | _ ->
                // TODO: check if there can be other patterns like array or list pat?
                false)

module FcsMfvUtil =
    let getFunctionReturnType parameters (mfv: FSharpMemberOrFunctionOrValue) =
        let rec skipFunctionParameters remaining (fullType: FSharpType) =
            if remaining = 0 then fullType
            else
                skipFunctionParameters (remaining - 1) fullType.GenericArguments[1]

        let returnType = skipFunctionParameters parameters mfv.FullType
        returnType

    let getFunctionParameterTypes parameters (mfv: FSharpMemberOrFunctionOrValue) =
        let result = Array.zeroCreate parameters
        let mutable fullType = mfv.FullType

        for i = 0 to parameters - 1 do
            result[i] <- fullType.GenericArguments[0]
            fullType <- fullType.GenericArguments[1]

        result

module AnnotationUtil2 =

    let private addParens forceParens (pattern: IFSharpPattern) =
        if forceParens || isNull (TuplePatNavigator.GetByPattern(pattern)) then
            let factory = pattern.CreateElementFactory()
            let parenPat = factory.CreateParenPat()
            parenPat.SetPattern(pattern) |> ignore

    let addSpaceBeforeColon forceSpaceBeforeColon (pattern: ITreeNode) =
        if forceSpaceBeforeColon || pattern.GetSettingsStoreWithEditorConfig().GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(pattern, Whitespace()) |> ignore

    let addTypeUsage typeString (node: ITreeNode) =
        let factory = node.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString)
        ModificationUtil.AddChildAfter(node, factory.CreateReturnTypeInfo(typeUsage))

    let replaceWithTypedPattern typeString (pattern: IFSharpPattern) =
        let factory = pattern.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString)
        ModificationUtil.ReplaceChild(pattern, factory.CreateTypedPat(pattern, typeUsage))

    let specifyPattern displayContext (fcsType: FSharpType) forceParens (pattern: IFSharpPattern) =
        let typeString = fcsType.Format(displayContext)

        pattern
        |> PatUtil2.removeReturnTypeInfo
        |> PatUtil2.removeInnerParens
        |> replaceWithTypedPattern typeString
        |> addParens forceParens

    let specifyMethodReturnType displayContext (mfv: FSharpMemberOrFunctionOrValue) (method: IMemberDeclaration) =
        let typeString = mfv.ReturnParameter.Type.Format(displayContext)
        let anchor = method.ParametersDeclarationsEnumerable.LastOrDefault()

        anchor
        |> addTypeUsage typeString
        |> addSpaceBeforeColon false

    // can this be just binding ?
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
                let fcsType = FcsMfvUtil.getFunctionReturnType (parameters.Count()) mfv
                fcsType, parameters.LastOrDefault()

        let typeString = fcsType.Format(displayContext)
        let forceSpaceBeforeColon = anchor :? IPostfixTypeParameterDeclarationList

        anchor
        |> addTypeUsage typeString
        |> addSpaceBeforeColon forceSpaceBeforeColon