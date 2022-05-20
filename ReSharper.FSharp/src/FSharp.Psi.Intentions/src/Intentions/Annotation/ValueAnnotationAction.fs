namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type private AnnotationContext =
    | ParameterContext of Pattern: IFSharpPattern * DisplayContext: FSharpDisplayContext * FSType: FSharpType
    | ValueContext of Pattern: IFSharpPattern * ForceParens: bool

[<ContextAction(Name = "AnnotateValue", Group = "F#",
                Description = "Annotate value or parameter with explicit type")>]
type ValueAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let mutable annotationContext = ValueNone

    override this.IsAvailable _ =
        // this is rather complex:
        // for parameter we can annotate anything we want, basically because we can get full type of member declaration
        // for value it seems that we can annotate only ref pats because we can't really get type from wild pat, or array pat or list pat
        // I couldn't find a way at least
        // but check for parameter is rather long
        // so maybe we should just stick to annotating ref pats only?

        let tryGetParameterAnnotationContextFromDeclaration (parameter: IParametersPatternDeclaration) (parameters: TreeNodeEnumerable<IParametersPatternDeclaration>) (declaration: IFSharpDeclaration) =
            match declaration with
            | Declaration.IsNotNullAndHasMfvSymbolUse(symbolUse, mfv) ->
                let parameterIndex = parameters |> Seq.findIndex (fun bindingParam -> bindingParam == parameter)
                let actualParameterType = FcsMfvUtil.getFunctionParameterAt parameterIndex  mfv.FullType
                annotationContext <- ValueSome (ParameterContext(parameter.Pattern, symbolUse.DisplayContext, actualParameterType))
                true
            | _ ->
                false

        let isAvailableForParameter() =
            let parameter = dataProvider.GetSelectedElement<IParametersPatternDeclaration>()
            isNotNull parameter
            && not (AnnotationUtil.isFullyAnnotatedPattern parameter.Pattern)
            &&  match parameter.Parent with
                | :? IBinding as binding ->
                    match binding.HeadPattern with
                    | :? IFSharpDeclaration as bindingDecl ->
                        tryGetParameterAnnotationContextFromDeclaration parameter binding.ParametersDeclarationsEnumerable bindingDecl
                    | _ ->
                        false

                | :? IMemberDeclaration as memberDecl ->
                   tryGetParameterAnnotationContextFromDeclaration parameter memberDecl.ParametersDeclarationsEnumerable memberDecl
                | _ ->
                    false

        let isAvailableForValue() =
            let binding = dataProvider.GetSelectedElement<IBinding>()
            let pattern = dataProvider.GetSelectedElement<IFSharpPattern>()

            if isNull binding || isNull pattern then false else

            let pattern =
                match pattern.Parent with
                | :? ITuplePat as tuplePat ->
                    tuplePat :> IFSharpPattern
                | _ ->
                    pattern

            let isAvailable =
                AnnotationUtil.isValueBinding binding
                && StandaloneAnnotationUtil.isSupportedPatternForStandaloneAnnotation pattern

            if isAvailable then
                let forceParens =
                    pattern :? ITuplePat
                    || match binding.BindingKeyword.GetText() with
                       | "let!" | "and!" | "use!" ->
                           true
                       | _ ->
                           false
                annotationContext <- ValueSome (ValueContext(pattern, forceParens))
                true
            else
                false

        isAvailableForParameter()
        || isAvailableForValue()

    override this.Text = "Add value type annotations"

    override x.ExecutePsiTransaction _ =
        match annotationContext with
        | ValueNone ->
            failwith "impossible" // TODO: better assert
        | ValueSome (ParameterContext(pattern, context, fsType)) ->
            use writeCookie = WriteLockCookie.Create(pattern.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()
            SpecifyUtil.specifyPattern context fsType true pattern
        | ValueSome (ValueContext(pattern, forceParens)) ->
            use writeCookie = WriteLockCookie.Create(pattern.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()
            StandaloneAnnotationUtil.specifyPatternThatSupportsStandaloneAnnotation forceParens pattern