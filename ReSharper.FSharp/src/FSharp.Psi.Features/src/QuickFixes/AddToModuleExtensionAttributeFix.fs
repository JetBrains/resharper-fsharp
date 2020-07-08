namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open FSharpAttributesUtil

type AddToModuleExtensionAttributeFix(warning: ExtensionMemberInNonExtensionTypeWarning) =
    inherit FSharpQuickFixBase()

    let mutable moduleName = null

    do moduleName <- LetModuleDeclNavigator.GetByAttribute(warning.Attr).GetContainingTypeDeclaration()
    override x.Text = sprintf "Add 'Extension' attribute to '%s' module" moduleName.DeclaredName

    override x.IsAvailable _ =
        match moduleName with
        | :? INamedModuleDeclaration -> false
        | _ -> isValid warning.Attr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(warning.Attr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let attribute = warning.Attr.CreateElementFactory().CreateAttribute("Extension")
        match moduleName with
        | :? IDeclaredModuleDeclaration as m ->
            if m.AttributeLists.IsEmpty then addAttributesList m true
            addAttribute m.AttributeLists.[0] attribute
        | :? IObjectTypeDeclaration as o ->
            let attributeList = getAttributeList o
            addAttribute attributeList attribute
        | _ -> ()
