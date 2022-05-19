namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.DataProviders
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2

[<ContextAction(Name = "AnnotateFunction2",
                Group = "F#",
                Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction2(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let isAnnotated (binding: IBinding) =
        isNotNull binding.ReturnTypeInfo
        && binding.ParametersDeclarationsEnumerable
           |> Seq.forall (fun parameter ->
               let pattern = parameter.Pattern.IgnoreInnerParens()

               match pattern with
               | :? ITypedPat as typedPat ->
                   PatUtil2.isPartiallyAnnotatedTypedPat typedPat
               | :? IUnitPat ->
                   true
               | _ ->
                   false)

    override x.Text = "Add function type annotations"

    override x.IsAvailable _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()

        if isNull binding then
            false
        else

            // TODO: is function binding helper

            (not (binding.ParametersDeclarationsEnumerable.IsEmpty())
             || not (binding.HeadPattern :? ILocalReferencePat)) // function in class ?
            && isAtBindingKeywordOrReferencePatternOrGenericParameters dataProvider binding
            && not (isAnnotated binding)

    override x.ExecutePsiTransaction _ =
        let binding = dataProvider.GetSelectedElement<IBinding>() // IBinding?

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match binding.HeadPattern.As<IReferencePat>() with
        | PatUtil2.HasMfvSymbolUse (symbolUse, mfv) ->

            let displayContext = symbolUse.DisplayContext
            let parameters = binding.ParametersDeclarations

            if parameters.Count > 0 then
                let types = FcsMfvUtil.getFunctionParameterTypes parameters.Count mfv

                (parameters, types)
                ||> Seq.iter2 (fun parameter fsType ->
                    SpecifyTypes.specifyParameterDeclaration displayContext fsType (ValueSome true) parameter)

            if isNull binding.ReturnTypeInfo then
                SpecifyTypes.specifyFunctionBindingReturnType displayContext mfv binding
        | _ ->
            ()