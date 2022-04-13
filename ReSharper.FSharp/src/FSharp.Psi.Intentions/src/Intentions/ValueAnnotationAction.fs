namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

// TODO: use something this if actions get merged or remove this
[<RequireQualifiedAccess>]
type private TypeAnnotationContext =
    | PartiallyAnnotatedTypedPat of ITypedPat
    | PartiallyAnnotatedReferencePat of ILocalReferencePat
    | WildPat of IWildPat
    | FunctionBinding of ILocalBinding
    | Tuple of ITuplePat
    | Property of IMemberDeclaration
    | Method of IMemberDeclaration
    | ArrayOrList of IArrayOrListPat

[<ContextAction(Name = "AnnotateBinding", Group = "F#",
                Description = "Annotate value or parameter with explicit type")>]
type ValueAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.IsAvailable _ =
        let localReference = dataProvider.GetSelectedElement<IReferencePat>() // Not a reference pat, others too?

        isNotNull localReference
        &&  match localReference.Parent with
            | :? ITypedPat as PatUtil.IsPartiallyAnnotatedTypedPat ->
                true
            | :? IUnitPat
            | :? ITypedPat ->
                false
            | :? ILocalBinding as localBinding when localBinding.ParameterPatterns.Count > 0 ->
                false
            | _ ->
                match localReference with
                | PatUtil.IsPartiallyAnnotatedLocalRefPat ->
                    true
                | _ ->
                    false

    override this.Text = "Add value type annotations"

    override x.ExecutePsiTransaction _ =
        let refPat = dataProvider.GetSelectedElement<ILocalReferencePat>()

        use writeCookie = WriteLockCookie.Create(refPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        SpecifyTypes.specifyReferencePat refPat