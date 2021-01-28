namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpAttributesUtil

type AddExtensionAttributeFix(warning: ExtensionMemberInNonExtensionTypeWarning) =
    inherit FSharpQuickFixBase()

    let [<Literal>] extensionNamespaceName = "System.Runtime.CompilerServices"
    let [<Literal>] attributeName = "Extension"

    let declaration =
        let attributeOwner: IFSharpTypeMemberDeclaration =
            match LetBindingsDeclarationNavigator.GetByAttribute(warning.Attr) with
            | null -> MemberDeclarationNavigator.GetByAttribute(warning.Attr) :> _
            | letModuleDec -> letModuleDec :> _
        if isNotNull attributeOwner then attributeOwner.GetContainingTypeDeclaration() else null

    override x.Text = sprintf "Add 'Extension' attribute to '%s'" declaration.SourceName

    override x.IsAvailable _ =
        match declaration with
        | :? ITopLevelModuleLikeDeclaration -> false
        | _ -> isValid declaration

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(warning.Attr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let attribute = warning.Attr.CreateElementFactory().CreateAttribute(attributeName)
        match declaration with
        | :? INestedModuleDeclaration as m ->
            match tryGetOpen m extensionNamespaceName with
            | Some _ ->
                let file = declaration.FSharpFile
                let settingsStore = file.GetSettingsStoreWithEditorConfig()
                addOpen (m.GetDocumentStartOffset()) file settingsStore extensionNamespaceName
            | None -> ()

            if m.AttributeLists.IsEmpty then
                addOuterAttributeList true m
            addAttribute m.AttributeLists.[0] attribute

        | :? IFSharpTypeOrExtensionDeclaration as t ->
            let attributeList = getTypeDeclarationAttributeList t
            addAttribute attributeList attribute
        | _ -> ()
