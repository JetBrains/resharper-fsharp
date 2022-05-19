namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type [<Struct>] private AnnotationContext =
    | AnnotationContext of Pattern: IFSharpPattern * Mfv: FSharpMemberOrFunctionOrValue * DisplayContext: FSharpDisplayContext

[<ContextAction(Name = "AnnotateValue", Group = "F#",
                Description = "Annotate value or parameter with explicit type")>]
type ValueAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let mutable annotationContext = ValueNone

    override this.IsAvailable _ =

        let isAvailableForParameter () =
            let parameter = dataProvider.GetSelectedElement<IParametersPatternDeclaration>()
            isNotNull parameter
            && not (AnnotationUtil.isFullyAnnotatedPattern parameter.Pattern)
            &&  match parameter.Parent with
                | :? IFSharpDeclaration as Declaration.HasMfvSymbolUse(symbolUse, mfv) ->
                    annotationContext <- ValueSome (AnnotationContext(parameter.Pattern, mfv, symbolUse.DisplayContext))
                    true
                | _ ->
                    false

        let isAvailableForBinding () =
            let binding = dataProvider.GetSelectedElement<IBinding>()
            isNotNull binding
            && AnnotationUtil.isValueBinding binding
            && not (AnnotationUtil.isFullyAnnotatedPattern binding.HeadPattern)
            &&  match binding.Parent with
                | :? IFSharpDeclaration as Declaration.HasMfvSymbolUse(symbolUse, mfv) ->
                    annotationContext <- ValueSome (AnnotationContext(binding.HeadPattern, mfv, symbolUse.DisplayContext))
                    true
                | _ ->
                    false

        isAvailableForParameter()
        || isAvailableForBinding()

    override this.Text = "Add value type annotations"

    override x.ExecutePsiTransaction _ =
        let pattern = dataProvider.GetSelectedElement<IReferencePat>()

        use writeCookie = WriteLockCookie.Create(pattern.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match annotationContext with
        | ValueNone ->
            failwith "impossible" // TODO: better assert
        | ValueSome (AnnotationContext(pattern, mfv, context)) ->
            SpecifyUtil.specifyPattern context mfv.FullType false pattern