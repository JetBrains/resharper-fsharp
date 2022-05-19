namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type [<Struct>] private AnnotationContext =
    | AnnotationContext of Mfv: FSharpMemberOrFunctionOrValue * DisplayContext: FSharpDisplayContext

[<ContextAction(Name = "AnnotateValue", Group = "F#",
                Description = "Annotate value or parameter with explicit type")>]
type ValueAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let mutable annotationContext = ValueNone

    override this.IsAvailable _ =

        // this isn't strictly about reference patterns, wilds, array or lists do count too
        // maybe this should be moved to process parameters only

        // IParameterPatternPat
        // IValue or whatever ?

        // we need to find a type of pattern

        // this is like one big todo

        // IParameterDeclaration
        // IBinding which is not a function

        let localReference = dataProvider.GetSelectedElement<IReferencePat>() // what is this exactly? ParameterPattern / binding

        let isAvailable =
            isNotNull localReference
            && AnnotationUtil.isFullyAnnotatedPat localReference

        isAvailable
        &&  match localReference with
            | Declaration.HasMfvSymbolUse (symbolUse, mfv) ->
                annotationContext <- ValueSome (AnnotationContext(mfv, symbolUse.DisplayContext))
                true
            | _ ->
                false

    override this.Text = "Add value type annotations"

    override x.ExecutePsiTransaction _ =
        let pattern = dataProvider.GetSelectedElement<IReferencePat>()

        use writeCookie = WriteLockCookie.Create(pattern.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match annotationContext with
        | ValueNone ->
            failwith "impossible" // TODO: better assert
        | ValueSome (AnnotationContext(mfv, context)) ->
            SpecifyUtil.specifyPattern context mfv.FullType false pattern