namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Settings
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

[<ContextAction(Name = "AnnotateFunction", Group = "F#",
                Description = "Annotate function with parameter types and return type")>]
type SpecifyTypesAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let specifyParameterTypes
            (parameterOwner: IParametersOwnerPat) (factory: IFSharpElementFactory)
            (mfv: FSharpMemberOrFunctionOrValue) displayContext =

        let typedTreeParameters =
            parameterOwner.Parameters
            |> Seq.zip mfv.CurriedParameterGroups
            |> Seq.toList

        // Annotate function parameters
        for fcsParamsGroup, parameter in typedTreeParameters do
            let paramName =
                match parameter with
                | :? IReferencePat as ref -> ref
                | :? IParenPat as ref -> ref.Pattern.As<IReferencePat>()
                | _ -> null

            if isNull paramName then () else

            // If the parameter is not curried, there will be multiple types pertaining to it
            let subTypeUsages =
                fcsParamsGroup
                |> Seq.map (fun fcsType -> factory.CreateTypeUsage(fcsType.Type.Format(displayContext)))
                |> Seq.toArray

            let typedPat = factory.CreateTypedPat(paramName, factory.CreateTypeUsage(subTypeUsages))
            let parenPat = factory.CreateParenPat()
            parenPat.SetPattern(typedPat) |> ignore

            replaceWithCopy parameter parenPat

    let specifyReturnType
            (namedPat: INamedPat) (factory: IFSharpElementFactory) (mfv: FSharpMemberOrFunctionOrValue) displayContext =

        let typeUsage = factory.CreateTypeUsage(mfv.ReturnParameter.Type.Format(displayContext))
        let returnTypeInfo = ModificationUtil.AddChildAfter(namedPat, factory.CreateReturnTypeInfo(typeUsage))

        let settingsStore = namedPat.GetSettingsStoreWithEditorConfig()
        if settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(returnTypeInfo, Whitespace()) |> ignore

    let isAnnotated (binding: IBinding) =
        isNotNull binding.ReturnTypeInfo &&

        match binding.HeadPattern with
        | :? IParametersOwnerPat as parametersOwner ->
            parametersOwner.ParametersEnumerable |> Seq.forall (fun pat -> pat.IgnoreInnerParens() :? ITypedPat)
        | _ -> true

    override x.Text = "Annotate function with parameter types and return type"

    override x.IsAvailable _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        if isNull binding then false else

        let namedPat = binding.HeadPattern.As<INamedPat>()
        isNotNull namedPat && isNotNull namedPat.Identifier && not (isAnnotated binding)

    override x.ExecutePsiTransaction _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        let factory = binding.CreateElementFactory()

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let namedPat = binding.HeadPattern.As<INamedPat>()
        if isNull namedPat then () else

        let symbolUse = namedPat.GetFSharpSymbolUse()
        if isNull symbolUse then () else

        let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let displayContext = symbolUse.DisplayContext

        let parametersOwner = namedPat.As<IParametersOwnerPat>()
        if isNotNull parametersOwner then
            specifyParameterTypes parametersOwner factory mfv displayContext

        if isNull binding.ReturnTypeInfo then
            specifyReturnType namedPat factory mfv displayContext
