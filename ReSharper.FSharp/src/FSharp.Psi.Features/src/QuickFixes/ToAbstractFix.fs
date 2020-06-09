namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ToAbstractFix(error: NoImplementationGivenError) =
    inherit FSharpQuickFixBase()

    let typeDecl = error.TypeDecl

    let getAttributeList typeDecl =
        let typeDeclarationGroup = TypeDeclarationGroupNavigator.GetByTypeDeclaration(typeDecl)
        if typeDeclarationGroup.TypeDeclarations.[0] == typeDecl then
            let attributeLists = typeDeclarationGroup.AttributeLists
            if not attributeLists.IsEmpty then attributeLists.[0] else
            FSharpAttributesUtil.addAttributesList typeDeclarationGroup true; typeDeclarationGroup.AttributeLists.[0]
        else
            let attributeLists = typeDecl.AttributeLists
            if not attributeLists.IsEmpty then attributeLists.[0] else
            FSharpAttributesUtil.addAttributesList typeDecl false; typeDecl.AttributeLists.[0]

    override x.Text = "To abstract"

    override x.IsAvailable _ =
        isValid typeDecl

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(typeDecl.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let attributeList = getAttributeList typeDecl
        let attribute = typeDecl.CreateElementFactory().CreateAttribute("AbstractClass")
        FSharpAttributesUtil.addAttribute attributeList attribute
