namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ContextAction(Name = "AnnotateFunctionArgumentTypes", Group = "F#",
                Description = "Annotate function with parameter types")>]
type FunctionArgumentTypesAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit SpecifyTypes.FunctionAnnotationActionBase(dataProvider)

    override this.IsAnnotated (binding: IBinding) =
        match binding.HeadPattern with
        | :? IParametersOwnerPat as parametersOwner ->
            parametersOwner.ParametersEnumerable |> Seq.forall (fun pat -> pat.IgnoreInnerParens() :? ITypedPat)
        | _ -> true

    override this.Text = "Add parameter type annotations"
    override this.ApplyFunctionAnnotation parametersOwner binding mfv displayContext =
        if isNotNull parametersOwner then
            SpecifyTypes.specifyArgumentTypes parametersOwner binding mfv displayContext    
