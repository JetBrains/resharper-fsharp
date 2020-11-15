namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Resources.Shell

module SpecifyTypes =
    [<AbstractClass>]
    type FunctionAnnotationActionBase(dataProvider: FSharpContextActionDataProvider) =
        inherit FSharpContextActionBase(dataProvider)
        
        abstract member IsAnnotated: IBinding -> bool
        abstract member ApplyFunctionAnnotation: IParametersOwnerPat -> IBinding -> FSharpMemberOrFunctionOrValue -> FSharpDisplayContext -> unit
        
        override this.IsAvailable _ =
            let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
            if isNull letBindings then false else

            let bindings = letBindings.Bindings
            if bindings.Count <> 1 then false else

            isAtLetExprKeywordOrNamedPat dataProvider letBindings &&
                bindings |> Seq.head |> this.IsAnnotated |> not
        override this.ExecutePsiTransaction _ =
            let binding = dataProvider.GetSelectedElement<IBinding>()

            use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()

            let namedPat = binding.HeadPattern.As<INamedPat>()
            if isNull namedPat then () else

            let symbolUse = namedPat.GetFSharpSymbolUse()
            if isNull symbolUse then () else

            let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
            let displayContext = symbolUse.DisplayContext

            let parametersOwner = namedPat.As<IParametersOwnerPat>()
            this.ApplyFunctionAnnotation parametersOwner binding mfv displayContext
    let specifyArgumentTypes
            (parameterOwner: IParametersOwnerPat) 
            (binding: IBinding)
            (mfv: FSharpMemberOrFunctionOrValue) 
            (displayContext: FSharpDisplayContext) =
        let factory = binding.CreateElementFactory()

        let addParens pattern =
            let parenPat = factory.CreateParenPat()
            parenPat.SetPattern(pattern) |> ignore
            parenPat :> IFSharpPattern

        let types = FcsTypesUtil.getFunctionTypeArgs mfv.FullType
        let parameters = parameterOwner.Parameters

        for fcsType, parameter in (types, parameters) ||> Seq.zip do
            match parameter.IgnoreInnerParens() with
            | :? IConstPat | :? ITypedPat -> ()
            | pattern ->

            let pattern =
                match pattern with
                | :? ITuplePat -> addParens pattern
                | _ -> pattern

            let typedPat = factory.CreateTypedPat(pattern, factory.CreateTypeUsage(fcsType.Format(displayContext)))
            let parenPat = addParens typedPat

            replaceWithCopy parameter parenPat

    let specifyBindingReturnType
        (binding: IBinding)
        (mfv: FSharpMemberOrFunctionOrValue)
        (displayContext: FSharpDisplayContext) =
        let typeString =
            let fullType = mfv.FullType
            if fullType.IsFunctionType then
                let specifiedTypesCount =
                    match binding.HeadPattern with
                    | :? IParametersOwnerPat as pat -> pat.Parameters.Count
                    | _ -> 0

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

        let pat = binding.HeadPattern
        let returnTypeInfo = ModificationUtil.AddChildAfter(pat, factory.CreateReturnTypeInfo(typeUsage))

        let settingsStore = pat.GetSettingsStoreWithEditorConfig()
        if settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon) then
            ModificationUtil.AddChildBefore(returnTypeInfo, Whitespace()) |> ignore