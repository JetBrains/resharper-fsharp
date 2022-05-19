namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "AnnotateBinding", Group = "F#",
                Description = "Annotate value or parameter with explicit type")>]
type ValueAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.IsAvailable _ =

        // this isn't strictly about reference patterns, wilds, array or lists do count too
        // maybe this should be moved to process parameters only

        let localReference = dataProvider.GetSelectedElement<ILocalReferencePat>()

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

        SpecifyTypes.specifyReferencePat ValueNone refPat