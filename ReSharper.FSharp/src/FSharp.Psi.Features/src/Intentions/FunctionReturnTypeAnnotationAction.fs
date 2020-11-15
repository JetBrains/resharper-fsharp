namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ContextAction(Name = "AnnotateFunctionReturnType", Group = "F#",
                Description = "Annotate function with return type")>]
type FunctionReturnTypeAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit SpecifyTypes.FunctionAnnotationActionBase(dataProvider)

    override this.IsAnnotated (binding: IBinding) = isNotNull binding.ReturnTypeInfo

    override x.Text = "Add return type annotation"
    override this.ApplyFunctionAnnotation _parametersOwner binding mfv displayContext =
        if isNull binding.ReturnTypeInfo then
            SpecifyTypes.specifyBindingReturnType binding mfv displayContext
