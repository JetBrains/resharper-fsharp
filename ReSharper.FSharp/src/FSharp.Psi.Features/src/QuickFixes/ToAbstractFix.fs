namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpAttributesUtil
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ToAbstractFix(error: NoImplementationGivenTypeError) =
    inherit FSharpQuickFixBase()

    let typeDecl = error.TypeDecl

    override x.Text = "To abstract"

    override x.IsAvailable _ =
        isValid typeDecl

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(typeDecl.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let attributeList = getTypeDeclarationAttributeList typeDecl
        let attribute = typeDecl.CreateElementFactory().CreateAttribute("AbstractClass")
        addAttribute attributeList attribute
