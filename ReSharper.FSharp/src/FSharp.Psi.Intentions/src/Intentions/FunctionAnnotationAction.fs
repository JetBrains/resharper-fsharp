namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util

/// This is a type used to supply "hints" to annotation functions to skip manual type searching
type [<Struct>] TypeHint = {
    FSType: FSharpType
    DisplayContext: FSharpDisplayContext
    AddParens: bool voption
}

module PatUtil =
    // TODO: there should be an extension for this
    let tryParentOrReturnSelf<'a when 'a :> IFSharpPattern> (value: IFSharpPattern) =
        match value.Parent with
        | :? 'a as resultPat -> resultPat :> IFSharpPattern
        | _ -> value

    let getReturnTypeInfo (localRef: IReferencePat) =
        // TODO: there should be an extension for this
        let rec tryNextSibling (ref: ITreeNode) =
            match ref.NextSibling with
            | null ->
                ValueNone
            | :? IReturnTypeInfo as returnTypeInfo ->
                ValueSome returnTypeInfo
            | sibling ->
                tryNextSibling sibling

        tryNextSibling localRef

    [<return: Struct>]
    let (|HasMfvSymbolUse|_|) (referencePat: IReferencePat) =
        if isNull referencePat then ValueNone else
        match referencePat.GetFcsSymbol() with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            ValueSome mfv
        | _ ->
            ValueNone

    [<return: Struct>]
    let (|IsPartiallyAnnotatedNamedTypeUsage|_|) (namedTypeUsage: INamedTypeUsage) =
        let anonUsageExists =
            isNotNull namedTypeUsage.ReferenceName
            && isNotNull namedTypeUsage.ReferenceName.TypeArgumentList
            && namedTypeUsage.ReferenceName.TypeArgumentList.TypeUsagesEnumerable
               |> Seq.exists (fun typeUsage -> typeUsage :? IAnonTypeUsage)
        if anonUsageExists then
            ValueSome()
        else
            ValueNone

    [<return: Struct>]
    let (|IsPartiallyAnnotatedReturnTypeInfo|_|) (typedPat: IReturnTypeInfo) =
        match typedPat.ReturnType with
        | :? INamedTypeUsage as IsPartiallyAnnotatedNamedTypeUsage ->
            ValueSome()
        | _ ->
            ValueNone

    [<return: Struct>]
    let (|IsPartiallyAnnotatedTypedPat|_|) (typedPat: ITypedPat) =
        match typedPat.TypeUsage with
        | :? INamedTypeUsage as IsPartiallyAnnotatedNamedTypeUsage ->
            ValueSome()
        | :? IFunctionTypeUsage as functionTypeUsage ->
            match functionTypeUsage.ArgumentTypeUsage, functionTypeUsage.ReturnTypeUsage with
            | :? INamedTypeUsage as IsPartiallyAnnotatedNamedTypeUsage, _
            | _, (:? INamedTypeUsage as IsPartiallyAnnotatedNamedTypeUsage) ->
                ValueSome()
            | _ ->
                ValueNone
        | _ ->
            ValueNone

    [<return: Struct>]
    let (|IsPartiallyAnnotatedLocalRefPat|_|) (localReference: IReferencePat) =
        match getReturnTypeInfo localReference with
        | ValueSome IsPartiallyAnnotatedReturnTypeInfo ->
            ValueSome()
        | ValueNone ->
            ValueSome()
        | _ ->
            ValueNone

    let rec private isPartiallyAnnotatedTuplePat (localReference: ITuplePat) =
        localReference.Patterns
        |> Seq.exists (fun pattern ->
            match pattern with
            | :? ILocalReferencePat as IsPartiallyAnnotatedLocalRefPat ->
             true
            | :? ITuplePat as tuplePat ->
                isPartiallyAnnotatedTuplePat tuplePat
            | _ ->
                false)

    [<return: Struct>]
    let (|IsPartiallyAnnotatedTuplePat|_|) (localReference: ITuplePat) =
        if isPartiallyAnnotatedTuplePat localReference then
            ValueSome()
        else
            ValueNone

module SpecifyTypes =

    let specifyMethodReturnType displayContext (mfv: FSharpMemberOrFunctionOrValue) (method: IMemberDeclaration) =
        let typeString = mfv.ReturnParameter.Type.Format(displayContext)
        let anchor = method.ParametersDeclarations.Last()

        let factory = method.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString)

        let returnTypeInfo = ModificationUtil.AddChildAfter(anchor, factory.CreateReturnTypeInfo(typeUsage))

        let settingsStore = anchor.GetSettingsStoreWithEditorConfig()

        if settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(returnTypeInfo, Whitespace()) |> ignore

    let specifyFunctionBindingReturnType displayContext (mfv: FSharpMemberOrFunctionOrValue) (binding: IBinding) =
        let typeString, anchor =
            let parameters = binding.ParametersDeclarations
            if parameters.Count > 0 then
                let rec skipFunctionParameters remaining (fullType: FSharpType) =
                    if remaining = 0 then fullType
                    else
                        skipFunctionParameters (remaining - 1) fullType.GenericArguments[1]

                let returnType = skipFunctionParameters parameters.Count mfv.FullType
                returnType.Format(displayContext), parameters.Last() :> ITreeNode
            else
                let headPat = binding.HeadPattern
                let headPatOrGenericParameterDeclaration =
                    match binding.HeadPattern.GetNextMeaningfulSibling() with
                    | :? IPostfixTypeParameterDeclarationList as typeParam ->
                        typeParam :> ITreeNode
                    | _ ->
                        headPat

                mfv.FullType.Format(displayContext), headPatOrGenericParameterDeclaration

        let factory = binding.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString)

        let returnTypeInfo = ModificationUtil.AddChildAfter(anchor, factory.CreateReturnTypeInfo(typeUsage))

        let settingsStore = anchor.GetSettingsStoreWithEditorConfig()

        // Unconditionally add space before ">" token or else compilation will fail
        if anchor :? IPostfixTypeParameterDeclarationList
           || settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(returnTypeInfo, Whitespace()) |> ignore

    let private addParens (factory: IFSharpElementFactory) (pattern: IFSharpPattern) =
        let parenPat = factory.CreateParenPat()
        parenPat.SetPattern(pattern) |> ignore
        parenPat :> IFSharpPattern

    let specifyPattern (typeHint: TypeHint) (patternToAnnotate: IFSharpPattern) (patternToChange: IFSharpPattern) =
        let needParens = typeHint.AddParens |> ValueOption.defaultValue false
        let patternToChange = patternToAnnotate.IgnoreParentParens()
        let factory = patternToAnnotate.CreateElementFactory()
        let annotatedPattern =
            let typedPat = factory.CreateTypedPat(patternToAnnotate, factory.CreateTypeUsage(typeHint.FSType.Format(typeHint.DisplayContext)))
            if needParens then
                addParens factory typedPat
            else
                typedPat :> _

        ModificationUtil.ReplaceChild(patternToChange, annotatedPattern) |> ignore

    let specifyArrayOrListPat (arrayOrListPat: IArrayOrListPat) =
        let symbolUse =
            let checker = arrayOrListPat.FSharpFile.FcsCheckerService
            checker.ResolveNameAtLocation(arrayOrListPat, [| |], false, "Get declaration")

        match symbolUse with
        | Some symbolUse when (symbolUse.Symbol :? FSharpMemberOrFunctionOrValue) ->
            let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
            let displayContext = symbolUse.DisplayContext
            ()
        | _ ->
            ()

    let specifyWildPat (wildPat: IWildPat) =
        let symbolUse =
            let checker = wildPat.FSharpFile.FcsCheckerService
            checker.ResolveNameAtLocation(wildPat, [| "Wild" |], false, "Get declaration")

        match symbolUse with
        | Some symbolUse when (symbolUse.Symbol :? FSharpMemberOrFunctionOrValue) ->
            let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
            let displayContext = symbolUse.DisplayContext
            ()
        | _ ->
            ()

    let specifyReferencePat (typeHint: TypeHint voption) (refPat: IReferencePat) =

        let typeHint =
            match typeHint with
            | ValueSome _ ->
                typeHint
            | _ ->
                let symbolUse = refPat.GetFcsSymbolUse()
                if isNull symbolUse then ValueNone else

                let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
                let displayContext = symbolUse.DisplayContext
                ValueSome { FSType = mfv.FullType; DisplayContext = displayContext; AddParens = ValueNone }

        match typeHint with
        | ValueNone -> ()
        | ValueSome typeHint ->

        let symbolUse = refPat.GetFcsSymbolUse()
        if isNull symbolUse then () else

        let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let displayContext = symbolUse.DisplayContext

        let patternToChange =
            match refPat.IgnoreInnerParens() with
            // TODO: not sure this is needed any more
            | :? ITypedPat as partiallyTypedPat ->
                partiallyTypedPat :> IFSharpPattern

            | :? ILocalReferencePat as localRef ->
                // remove partially typed ReturnTypeInfo because it is not a part of a pattern
                let rec removeReturnTypeInfo (ref: ITreeNode) =
                    match ref.NextSibling with
                    | null ->
                        ()
                    | :? IReturnTypeInfo as returnTypeInfo ->
                        ModificationUtil.DeleteChild(returnTypeInfo)
                    | sibling ->
                        removeReturnTypeInfo sibling

                removeReturnTypeInfo localRef
                localRef :> IFSharpPattern

            | pattern ->
                pattern

        let addParens =
            match typeHint.AddParens with
            | ValueSome addParens -> addParens
            | _ ->
                match refPat.Parent with
                | :? ITuplePat as tuplePatParent ->
                    match tuplePatParent.Parent with
                    | :? IParenPat ->
                        false
                    | _ ->
                    true

                | _ ->
                    match refPat.Binding with
                    | :? IBinding as localBinding ->
                        match localBinding.BindingKeyword.GetText() with
                        | "let!" | "and!" | "use!" ->
                            true
                        | _ ->
                            false
                    | _ ->
                        true

        specifyPattern { typeHint with AddParens = ValueSome addParens } refPat patternToChange

    let private getTupleChildrenRefPatsReferences (pattern: ITuplePat) =
        [|
            for pattern in pattern.Patterns do
                let refPat =
                    match pattern.IgnoreInnerParens() with
                    | :? IReferencePat as localRef ->
                        localRef
                    | :? ITypedPat as typedPat when (typedPat.Pattern :? IReferencePat) ->
                        typedPat.Pattern :?> IReferencePat
                    | _ ->
                        null

                if isNotNull refPat then
                    let symbol = refPat.GetFcsSymbolUse()
                    if isNotNull symbol then
                        struct (refPat, symbol)
        |]

    let rec specifyTuplePat (hint: TypeHint voption) (pattern: ITuplePat) =

        let referenceList = ResizeArray(pattern.Patterns.Count)

        let rec getRefPats (pattern: ITuplePat) =
            let childrenReferences = getTupleChildrenRefPatsReferences pattern
            referenceList.Add(struct (pattern, childrenReferences))
            for pattern in pattern.Patterns do
                match pattern.IgnoreInnerParens() with
                | :? ITuplePat as nestedTuplePat ->
                    getRefPats nestedTuplePat
                | _ ->
                    ()

        getRefPats pattern

        for i = referenceList.Count - 1 downto 0 do
            let struct (tuplePat, refPats) = referenceList[i]
            for refPat, symbolUse in refPats do
                specifyReferencePat ValueNone refPat

            match tuplePat.IgnoreParentParens() with
            | :? IParenPat ->
                ()
            | _ ->
                let factory = tuplePat.CreateElementFactory()
                let tuplePatternWithPats = addParens factory tuplePat
                ModificationUtil.ReplaceChild(tuplePat, tuplePatternWithPats) |> ignore

    let specifyPropertyType displayContext (fcsType: FSharpType) (decl: IMemberDeclaration) =
        Assertion.Assert(isNull decl.ReturnTypeInfo, "isNull decl.ReturnTypeInfo")
        Assertion.Assert(decl.ParametersDeclarationsEnumerable.IsEmpty(),
            "decl.ParametersDeclarationsEnumerable.IsEmpty()")

        let factory = decl.CreateElementFactory()
        let returnTypeInfo = factory.CreateReturnTypeInfo(factory.CreateTypeUsage(fcsType.Format(displayContext)))
        ModificationUtil.AddChildAfter(decl.Identifier, returnTypeInfo) |> ignore

    let specifyParameterDeclaration displayContext (fcsType: FSharpType) addParens (parameter: IParametersPatternDeclaration) =
        let patternToAnnotate = parameter.Pattern.IgnoreInnerParens()
        let patternToChange = parameter.Pattern

        match parameter.Pattern.IgnoreInnerParens() with
        | :? ITypedPat as typedPat ->
            match typedPat with
            | PatUtil.IsPartiallyAnnotatedTypedPat ->
                match typedPat.Pattern.IgnoreInnerParens() with
                | :? ILocalReferencePat as localRef ->
                    specifyReferencePat ValueNone localRef
                | _ ->
                    specifyPattern { TypeHint.DisplayContext = displayContext; FSType = fcsType; AddParens = addParens } patternToAnnotate patternToChange
            | _ ->
                ()

        | :? ILocalReferencePat as localRef ->
            specifyReferencePat ValueNone localRef

        | :? ITuplePat as tuplePat ->
            specifyTuplePat ValueNone tuplePat

        | :? IConstPat | :? IUnitPat ->
            ()

        | _ ->
            specifyPattern { TypeHint.DisplayContext = displayContext; FSType = fcsType; AddParens = addParens } patternToAnnotate patternToChange

// function annotation or member annotation
// value annotation or parameter annotation -> ...

// 1. basic check
// 2. try to get type hint
// 3. save to a context or return false

[<ContextAction(Name = "AnnotateFunction", Group = "F#",
                Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let isAnnotated (binding: IBinding) =
        isNotNull binding.ReturnTypeInfo &&
        binding.ParametersDeclarations |> Seq.forall (fun parameter ->
            let pattern = parameter.Pattern.IgnoreInnerParens()
            match pattern with
            | :? ITypedPat as PatUtil.IsPartiallyAnnotatedTypedPat -> false
            | :? ITypedPat | :? IUnitPat -> true
            | _ -> false)

    override x.Text = "Add function type annotations"

    override x.IsAvailable _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        if isNull binding then false else

        // TODO: is function binding helper

        (binding.ParametersDeclarations.Count > 0 || not (binding.HeadPattern :? ILocalReferencePat))
        && isAtBindingKeywordOrReferencePatternOrGenericParameters dataProvider binding && not (isAnnotated binding)

    override x.ExecutePsiTransaction _ =
        let binding = dataProvider.GetSelectedElement<IBinding>() // IBinding?

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let refPat = binding.HeadPattern.As<IReferencePat>()
        if isNull refPat then () else

        let symbolUse = refPat.GetFcsSymbolUse()
        if isNull symbolUse then () else

        let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let displayContext = symbolUse.DisplayContext

        if binding.ParametersDeclarations.Count > 0 then
            let types = FcsTypeUtil.getFunctionTypeArgs false mfv.FullType
            (binding.ParametersDeclarations, types)
            ||> Seq.iter2 (fun parameter fsType -> SpecifyTypes.specifyParameterDeclaration displayContext fsType (ValueSome true) parameter)

        if isNull binding.ReturnTypeInfo then
            SpecifyTypes.specifyFunctionBindingReturnType displayContext mfv binding