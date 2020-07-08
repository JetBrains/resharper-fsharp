namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open FSharpAttributesUtil

type AddExtensionAttributeFix(warning: ExtensionMemberInNonExtensionTypeWarning) =
    inherit FSharpQuickFixBase()

    let declaration = LetModuleDeclNavigator.GetByAttribute(warning.Attr).GetContainingTypeDeclaration()

    override x.Text =
        match declaration with
        | :? IDeclaredModuleDeclaration as m ->
            sprintf "Add 'Extension' attribute to '%s' module" m.DeclaredName
        | :? IObjectTypeDeclaration as o ->
            sprintf "Add 'Extension' attribute to '%s' type" o.DeclaredName
        | _ -> ""

    override x.IsAvailable _ =
        match declaration with
        | :? ITopLevelModuleLikeDeclaration -> false
        | _ -> isValid warning.Attr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(warning.Attr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let attribute = warning.Attr.CreateElementFactory().CreateAttribute("Extension")
        match declaration with
        | :? IDeclaredModuleDeclaration as m ->
            if m.AttributeLists.IsEmpty then addAttributesList m true
            addAttribute m.AttributeLists.[0] attribute
        | :? IObjectTypeDeclaration as o ->
            let attributeList = getAttributeList o
            addAttribute attributeList attribute
        | _ -> ()
