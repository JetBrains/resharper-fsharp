module JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2

open FSharp.Compiler.Symbols
open JetBrains.Diagnostics
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
    let (|HasMfvSymbolUse|_|) (declaration: IFSharpDeclaration) =
        if isNull declaration then ValueNone else
        let symbolUse = declaration.GetFcsSymbolUse()
        if isNull symbolUse then ValueNone else

        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            ValueSome (symbolUse, mfv)
        | _ ->
            ValueNone

module PatUtil =

    let private findSibling<'node when 'node :> ITreeNode> node =
        let rec find (node: ITreeNode) =
            match node.NextSibling with
            | null ->
                ValueNone
            | :? 'node as result ->
                ValueSome result
            | sibling ->
                find sibling

        find node

    let getTypeUsage (pattern: IFSharpPattern) =
        findSibling<ITypeUsage> pattern

    let removeTypeUsage (pattern: IFSharpPattern) =
        match getTypeUsage pattern with
        | ValueSome typeUsage ->
            ModificationUtil.DeleteChild(typeUsage)
        | _ ->
            ()
        pattern

    let removeInnerParens (pattern: IFSharpPattern) =
        let updatedPattern = pattern.IgnoreInnerParens()
        if pattern == updatedPattern then
            pattern
        else
            ModificationUtil.ReplaceChild(pattern, updatedPattern)

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

module AnnotationUtil =

    let private isFullyAnnotatedNamedTypeUsage (namedTypeUsage: INamedTypeUsage) =
        isNull namedTypeUsage.ReferenceName ||
        isNull namedTypeUsage.ReferenceName.TypeArgumentList ||
        namedTypeUsage.ReferenceName.TypeArgumentList.TypeUsagesEnumerable
           |> Seq.exists (fun typeUsage -> typeUsage :? IAnonTypeUsage)
           |> not

    let rec private isFullyAnnotatedFunctionTypeUsage (functionTypeUsage: IFunctionTypeUsage) =
        isFullyAnnotatedTypeUsage functionTypeUsage.ArgumentTypeUsage
        && isFullyAnnotatedTypeUsage functionTypeUsage.ReturnTypeUsage

    and isFullyAnnotatedTypeUsage (typeUsage: ITypeUsage) =
        match typeUsage with
        | :? INamedTypeUsage as namedTypeUsage ->
            isFullyAnnotatedNamedTypeUsage namedTypeUsage
        | :? IFunctionTypeUsage as functionTypeUsage ->
            isFullyAnnotatedFunctionTypeUsage functionTypeUsage
        | _ -> false

    let rec isFullyAnnotatedPattern (pattern: IFSharpPattern) =
        match pattern.IgnoreInnerParens() with
        | :? IUnitPat ->
            true
        | :? ITypedPat as typedPat ->
            isFullyAnnotatedPattern typedPat.Pattern
        | pattern ->
            match PatUtil.getTypeUsage pattern with
            | ValueSome typeUsage ->
                isFullyAnnotatedTypeUsage typeUsage
            | ValueNone ->
                false

    let isFullyAnnotatedBinding (binding: IBinding) =
        isNotNull binding.ReturnTypeInfo
        && binding.ParametersDeclarationsEnumerable
           |> Seq.forall (fun parameter ->
               isFullyAnnotatedPattern parameter.Pattern)

    let isFullyAnnotatedMemberDeclaration (memberDeclaration: IMemberDeclaration) =
        isNotNull memberDeclaration.ReturnTypeInfo &&
        memberDeclaration.ParametersDeclarations
        |> Seq.forall (fun parameter ->
               isFullyAnnotatedPattern parameter.Pattern)

    // let f x ... =
    // let f<'a> = ...
    let isFunctionBinding (binding: IBinding) =
        binding.HasParameters
        || binding.HeadPattern.GetNextMeaningfulSibling() :? IPostfixTypeParameterDeclarationList

    // let x = y
    // let x = fun y -> ...
    let isValueBinding (binding: IBinding) =
       not (isFunctionBinding binding)

module SpecifyUtil =

    let private addParens (pattern: IFSharpPattern) =
        let factory = pattern.CreateElementFactory()
        let parenPat = factory.CreateParenPat()
        parenPat.SetPattern(pattern) |> ignore
        ModificationUtil.ReplaceChild(pattern, parenPat) |> ignore

    let private addSpaceBeforeColon forceSpaceBeforeColon (pattern: ITreeNode) =
        if forceSpaceBeforeColon || pattern.GetSettingsStoreWithEditorConfig().GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(pattern, Whitespace()) |> ignore

    let private addTypeUsage typeString (node: ITreeNode) =
        let factory = node.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString)
        ModificationUtil.AddChildAfter(node, factory.CreateReturnTypeInfo(typeUsage))

    let private replaceWithTypedPattern typeString (pattern: IFSharpPattern) =
        let factory = pattern.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString)
        ModificationUtil.ReplaceChild(pattern, factory.CreateTypedPat(pattern, typeUsage))

    let rec private specifyTuplePat displayContext (fcsType: FSharpType) (pattern: ITuplePat) =
        let innerPatterns = pattern.Patterns
        for i = 0 to innerPatterns.Count - 1 do
            specifyPattern displayContext fcsType.GenericArguments[i] false innerPatterns[i]
        addParens pattern

    and specifyPattern displayContext (fcsType: FSharpType) forceParens (pattern: IFSharpPattern) =
        match pattern
            |> PatUtil.removeTypeUsage
            |> PatUtil.removeInnerParens with
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
        |> addTypeUsage typeString
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
                let fcsType = FcsMfvUtil.getFunctionReturnType (parameters.Count()) mfv
                fcsType, parameters.LastOrDefault()

        let typeString = fcsType.Format(displayContext)
        let forceSpaceBeforeColon = anchor :? IPostfixTypeParameterDeclarationList

        anchor
        |> addTypeUsage typeString
        |> addSpaceBeforeColon forceSpaceBeforeColon

    let specifyPropertyType displayContext (fcsType: FSharpType) (decl: IMemberDeclaration) =
        Assertion.Assert(isNull decl.ReturnTypeInfo, "isNull decl.ReturnTypeInfo")
        Assertion.Assert(decl.ParametersDeclarationsEnumerable.IsEmpty(),
            "decl.ParametersDeclarationsEnumerable.IsEmpty()")

        let factory = decl.CreateElementFactory()
        let returnTypeInfo = factory.CreateReturnTypeInfo(factory.CreateTypeUsage(fcsType.Format(displayContext)))
        ModificationUtil.AddChildAfter(decl.Identifier, returnTypeInfo) |> ignore