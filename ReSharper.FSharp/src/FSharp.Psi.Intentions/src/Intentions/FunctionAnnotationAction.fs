namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

module SpecifyTypes =
    let specifyBindingReturnType displayContext (mfv: FSharpMemberOrFunctionOrValue) (binding: IBinding) =
        let typeString =
            let fullType = mfv.FullType
            if fullType.IsFunctionType then
                let specifiedTypesCount = binding.ParametersDeclarations.Count

                let types = FcsTypeUtil.getFunctionTypeArgs true fullType
                if types.Length <= specifiedTypesCount then mfv.ReturnParameter.Type.Format(displayContext) else

                let remainingTypes = types |> List.skip specifiedTypesCount
                remainingTypes
                |> List.map (fun fcsType ->
                    let typeString = fcsType.Format(displayContext)
                    if fcsType.IsFunctionType then sprintf "(%s)" typeString else typeString)
                |> String.concat " -> "
            else
                mfv.ReturnParameter.Type.Format(displayContext)

        let factory = binding.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString, TypeUsageContext.TopLevel)

        let parameters = binding.ParametersDeclarations
        let anchor =
            if parameters.IsEmpty then binding.HeadPattern :> ITreeNode
            else parameters.Last() :> _

        let returnTypeInfo = ModificationUtil.AddChildAfter(anchor, factory.CreateReturnTypeInfo(typeUsage))

        let settingsStore = anchor.GetSettingsStoreWithEditorConfig()
        if settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(returnTypeInfo, Whitespace()) |> ignore

    let private addParens (factory: IFSharpElementFactory) (pattern: IFSharpPattern) =
        let parenPat = factory.CreateParenPat()
        parenPat.SetPattern(pattern) |> ignore
        parenPat :> IFSharpPattern

    let specifyParameterType displayContext (fcsType: FSharpType) (pattern: IFSharpPattern) =
        let pattern = pattern.IgnoreParentParens()
        let factory = pattern.CreateElementFactory()

        let newPattern =
            match pattern.IgnoreInnerParens() with
            | :? ITuplePat as tuplePat -> addParens factory tuplePat
            | pattern -> pattern

        let typedPat =
            let typeUsage = factory.CreateTypeUsage(fcsType.Format(displayContext), TypeUsageContext.TopLevel)
            let typedPat = factory.CreateTypedPat(newPattern, typeUsage)
            if isNull (TuplePatNavigator.GetByPattern(pattern)) then
                addParens factory typedPat
            else
                typedPat :> _

        ModificationUtil.ReplaceChild(pattern, typedPat) |> ignore

    let specifyPropertyType displayContext (fcsType: FSharpType) (decl: IMemberDeclaration) =
        Assertion.Assert(isNull decl.ReturnTypeInfo, "isNull decl.ReturnTypeInfo")
        Assertion.Assert(decl.ParametersDeclarationsEnumerable.IsEmpty(),
            "decl.ParametersDeclarationsEnumerable.IsEmpty()")

        let factory = decl.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(fcsType.Format(displayContext), TypeUsageContext.TopLevel)
        let returnTypeInfo = factory.CreateReturnTypeInfo(typeUsage)
        ModificationUtil.AddChildAfter(decl.Identifier, returnTypeInfo) |> ignore

[<ContextAction(Name = "AnnotateFunction", Group = "F#",
                Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let specifyParameterTypes displayContext (binding: IBinding) (mfv: FSharpMemberOrFunctionOrValue) =
        let types = FcsTypeUtil.getFunctionTypeArgs true mfv.FullType
        let parameters = binding.ParametersDeclarations

        for fcsType, parameter in (types, parameters) ||> Seq.zip do
            match parameter.Pattern.IgnoreInnerParens() with
            | :? IConstPat | :? ITypedPat -> ()
            | pattern -> SpecifyTypes.specifyParameterType displayContext fcsType pattern

    let isAnnotated (binding: IBinding) =
        isNotNull binding.ReturnTypeInfo &&
        binding.ParametersDeclarations |> Seq.forall (fun p ->
            let pattern = p.Pattern.IgnoreInnerParens()
            pattern :? ITypedPat || pattern :? IUnitPat)

    override x.Text = "Add type annotations"

    override x.IsAvailable _ =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        if isNull letBindings then false else

        let bindings = letBindings.Bindings
        if bindings.Count <> 1 then false else

        isAtLetExprKeywordOrReferencePattern dataProvider letBindings && not (isAnnotated bindings[0])

    override x.ExecutePsiTransaction _ =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        let binding = letBindings.Bindings |> Seq.exactlyOne

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let refPat = binding.HeadPattern.As<IReferencePat>()
        if isNull refPat then () else

        let symbolUse = refPat.GetFcsSymbolUse()
        if isNull symbolUse then () else

        let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let displayContext = symbolUse.DisplayContext

        if binding.HasParameters then
            specifyParameterTypes displayContext binding mfv

        if isNull binding.ReturnTypeInfo then
            SpecifyTypes.specifyBindingReturnType displayContext mfv binding
