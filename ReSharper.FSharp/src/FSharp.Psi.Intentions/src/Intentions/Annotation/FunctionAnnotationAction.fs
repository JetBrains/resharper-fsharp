namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.DataProviders
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2

[<ContextAction(Name = "AnnotateFunction",
                Group = "F#",
                Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override x.Text = "Add function type annotations"

    override x.IsAvailable _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        isNotNull binding &&
        AnnotationUtil.isFunctionBinding binding &&
        isAtBindingKeywordOrReferencePatternOrGenericParameters dataProvider binding &&
        not (AnnotationUtil.isFullyAnnotatedBinding binding)

    override x.ExecutePsiTransaction _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match binding.HeadPattern.As<IReferencePat>() with
        | Declaration.IsNotNullAndHasMfvSymbolUse (symbolUse, mfv) ->

            let displayContext = symbolUse.DisplayContext
            let parameters = binding.ParametersDeclarations

            if parameters.Count > 0 then
                let types = FcsMfvUtil.getFunctionParameterTypes parameters.Count mfv.FullType

                (parameters, types)
                ||> Seq.iter2 (fun parameter fsType ->
                    let pattern = parameter.Pattern
                    if not (AnnotationUtil.isFullyAnnotatedPattern pattern) then
                        SpecifyUtil.specifyPattern displayContext fsType true pattern)

            if isNull binding.ReturnTypeInfo then
                SpecifyUtil.specifyFunctionBindingReturnType displayContext mfv binding
        | _ ->
            ()