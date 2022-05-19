namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "AnnotateBinding", Group = "F#",
                Description = "Annotate binding with explicit type")>]
type TupleAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.IsAvailable _ =
        let tuplePat = dataProvider.GetSelectedElement<ITuplePat>()
        // TODO: IsPartiallyAnnotated
        isNotNull tuplePat

    override this.Text = "Add tuple type annotations"

    override x.ExecutePsiTransaction _ =
        let tuplePat = dataProvider.GetSelectedElement<ITuplePat>()

        use writeCookie = WriteLockCookie.Create(tuplePat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        SpecifyTypes.specifyTuplePat ValueNone tuplePat