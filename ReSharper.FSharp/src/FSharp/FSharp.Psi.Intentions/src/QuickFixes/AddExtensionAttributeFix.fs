namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpAttributesUtil

type AddExtensionAttributeFix(warning: ExtensionMemberInNonExtensionTypeWarning) =
    inherit FSharpQuickFixBase()

    let [<Literal>] attributeName = "Extension"

    let declaration =
        let attributeOwner: IFSharpTypeMemberDeclaration =
            match LetBindingsDeclarationNavigator.GetByBinding(BindingNavigator.GetByAttribute(warning.Attr)) with
            | null -> MemberDeclarationNavigator.GetByAttribute(warning.Attr) :> _
            | letModuleDec -> letModuleDec :> _
        if isNotNull attributeOwner then attributeOwner.GetContainingTypeDeclaration() else null

    override x.Text = $"Add 'Extension' attribute to '{declaration.SourceName}'"

    override x.IsAvailable _ =
        isValid declaration

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(warning.Attr.IsPhysical())

        let attributeList = 
            match declaration with
            | :? IDeclaredModuleDeclaration as moduleDecl ->
                if moduleDecl.AttributeLists.IsEmpty then
                    addOuterAttributeList moduleDecl
                moduleDecl.AttributeLists[0]

            | :? IFSharpTypeOrExtensionDeclaration as t ->
                getTypeDeclarationAttributeList t

            | _ -> null

        if isNull attributeList then () else

        let attribute = warning.Attr.CreateElementFactory().CreateAttribute(attributeName)
        let attribute = addAttribute attributeList attribute
        
        let reference = attribute.ReferenceName.Reference
        let extensionTypeElement = attribute.GetPsiModule().GetPredefinedType().ExtensionAttribute.GetTypeElement()
        let importHelper = LanguageManager.Instance.GetService<IFSharpQuickFixUtilComponent>(attribute.Language)
        importHelper.BindTo(reference, extensionTypeElement) |> ignore
