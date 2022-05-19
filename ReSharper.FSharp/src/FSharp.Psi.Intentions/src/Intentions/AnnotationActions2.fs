module JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.Application.Settings

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
        match referencePat.GetFcsSymbol() with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            ValueSome mfv
        | _ ->
            ValueNone

    let getReturnTypeInfo (localRef: IReferencePat) =
        match localRef.GetNextMeaningfulSibling() with
        | :? IReturnTypeInfo as returnTypeInfo ->
            ValueSome returnTypeInfo
        | _ ->
            ValueNone

    let isPartiallyAnnotatedNamedTypeUsage (namedTypeUsage: INamedTypeUsage) =
        isNotNull namedTypeUsage.ReferenceName
        && isNotNull namedTypeUsage.ReferenceName.TypeArgumentList
        && namedTypeUsage.ReferenceName.TypeArgumentList.TypeUsagesEnumerable
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

    let isPartiallyAnnotatedRefPat (localReference: IReferencePat) =
        match getReturnTypeInfo localReference with
        | ValueSome returnTypeInfo ->
            isPartiallyAnnotatedReturnTypeInfo returnTypeInfo
        | ValueNone ->
            true

    let rec isPartiallyAnnotatedTuplePat (localReference: ITuplePat) =
        localReference.Patterns
        |> Seq.exists (fun pattern ->
            match pattern with
            | :? IReferencePat as referencePat ->
                isPartiallyAnnotatedRefPat referencePat
            | :? ITuplePat as tuplePat ->
                isPartiallyAnnotatedTuplePat tuplePat
            | _ ->
                // TODO: check if there can be other patterns like array or list pat?
                false)

module AnnotationUtil2 =

    let specifyTreeNode typeString forceSpaceBeforeColon (node: ITreeNode) =
        let factory = node.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString)

        let returnTypeInfo = ModificationUtil.AddChildAfter(node, factory.CreateReturnTypeInfo(typeUsage))

        let settingsStore = node.GetSettingsStoreWithEditorConfig()

        if forceSpaceBeforeColon || settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(returnTypeInfo, Whitespace()) |> ignore

    let specifyMethodReturnType displayContext (returnType: FSharpType) (method: IMemberDeclaration) =
        let typeString = returnType.Format(displayContext)
        let anchor = method.ParametersDeclarations.Last()
        anchor |> specifyTreeNode typeString false

    let specifyFunctionBindingReturnType displayContext (mfv: FSharpMemberOrFunctionOrValue) (binding: IBinding) =

        let fcsType, anchor =
            let parameters = binding.ParametersDeclarations
            if parameters.Count > 0 then
                let rec skipFunctionParameters remaining (fullType: FSharpType) =
                    if remaining = 0 then fullType
                    else
                        skipFunctionParameters (remaining - 1) fullType.GenericArguments[1]

                let returnType = skipFunctionParameters parameters.Count mfv.FullType
                returnType, parameters.Last() :> ITreeNode
            else
                let headPat = binding.HeadPattern
                let headPatOrGenericParameterDeclaration =
                    match binding.HeadPattern.GetNextMeaningfulSibling() with
                    | :? IPostfixTypeParameterDeclarationList as typeParam ->
                        typeParam :> ITreeNode
                    | _ ->
                        headPat

                mfv.FullType, headPatOrGenericParameterDeclaration

        let typeString = fcsType.Format(displayContext)
        let forceSpaceBeforeColor = anchor :? IPostfixTypeParameterDeclarationList
        anchor |> specifyTreeNode typeString forceSpaceBeforeColor