namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Settings
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
    let specifyBindingReturnType (binding: IBindingImplementation) (mfv: FSharpMemberOrFunctionOrValue) displayContext =
        let typeString =
            let fullType = mfv.FullType
            if fullType.IsFunctionType then
                let specifiedTypesCount = binding.ParametersPatterns.Count

                let types = FcsTypesUtil.getFunctionTypeArgs fullType
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
        let typeUsage = factory.CreateTypeUsage(typeString)

        let parameters = binding.ParametersPatterns
        let pat = if parameters.IsEmpty then binding.HeadPattern else parameters.Last().Pattern
        let returnTypeInfo = ModificationUtil.AddChildAfter(pat, factory.CreateReturnTypeInfo(typeUsage))

        let settingsStore = pat.GetSettingsStoreWithEditorConfig()
        if settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(returnTypeInfo, Whitespace()) |> ignore


[<ContextAction(Name = "AnnotateFunction", Group = "F#",
                Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let specifyParameterTypes
            (binding: IBindingImplementation) (factory: IFSharpElementFactory)
            (mfv: FSharpMemberOrFunctionOrValue) displayContext =

        let addParens pattern =
            let parenPat = factory.CreateParenPat()
            parenPat.SetPattern(pattern) |> ignore
            parenPat :> IFSharpPattern

        let types = FcsTypesUtil.getFunctionTypeArgs mfv.FullType
        let parameters = binding.ParametersPatterns

        for fcsType, parameter in (types, parameters) ||> Seq.zip do
            match parameter.Pattern.IgnoreInnerParens() with
            | :? IConstPat | :? ITypedPat -> ()
            | pattern ->

            let pattern =
                match pattern with
                | :? ITuplePat -> addParens pattern
                | _ -> pattern

            let typedPat = factory.CreateTypedPat(pattern, factory.CreateTypeUsage(fcsType.Format(displayContext)))
            let parenPat = addParens typedPat
            let p = ElementType.PARAMETERS_PATTERN_DECLARATION.Create()
            let p1 = ModificationUtil.AddChildBefore(parameter, p)
            ModificationUtil.AddChild(p1, parenPat.Copy()) |> ignore
            deleteChild parameter

    let isAnnotated (binding: IBindingImplementation) =
        isNotNull binding.ReturnTypeInfo &&
        binding.ParametersPatterns |> Seq.forall (fun p -> p.Pattern.IgnoreInnerParens() :? ITypedPat)

    override x.Text = "Add type annotations"

    override x.IsAvailable _ =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        if isNull letBindings then false else

        let bindings = letBindings.Bindings
        if bindings.Count <> 1 then false else

        let binding = bindings.[0].As<IBindingImplementation>()
        isNotNull binding && isAtLetExprKeywordOrNamedPat dataProvider letBindings && not (isAnnotated binding)

    override x.ExecutePsiTransaction _ =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        let binding = letBindings.Bindings |> Seq.exactlyOne :?> IBindingImplementation
        let factory = binding.CreateElementFactory()

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let refPat = binding.HeadPattern.As<IReferencePat>()
        if isNull refPat then () else

        let symbolUse = refPat.GetFSharpSymbolUse()
        if isNull symbolUse then () else

        let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let displayContext = symbolUse.DisplayContext

        if binding.HasParameters then
            specifyParameterTypes binding factory mfv displayContext

        if isNull binding.ReturnTypeInfo then
            SpecifyTypes.specifyBindingReturnType binding mfv displayContext
