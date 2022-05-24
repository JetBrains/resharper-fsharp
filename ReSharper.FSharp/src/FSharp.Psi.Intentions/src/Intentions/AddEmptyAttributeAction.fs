namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

module AttributeUtil =

    let isAvailable dataProvider decl =
        isNotNull decl &&
        isAtTreeNode dataProvider decl

    let addAttributeToEndOfList name (list: IAttributeList) =
        let factory = list.CreateElementFactory()
        let attribute =
            if String.IsNullOrWhiteSpace name then
                let attribute = factory.CreateAttribute("foo")
                ModificationUtil.DeleteChild(attribute.FSharpIdentifier) // strip foo because factory does not create empty attributes
                attribute
            else
                factory.CreateAttribute(name)

        if list.AttributesEnumerable.IsEmpty() then
            FSharpAttributesUtil.addAttribute list attribute |> ignore
        else
            FSharpAttributesUtil.addAttributeAfter (list.AttributesEnumerable.LastOrDefault()) attribute

        list

    let addEmptyAttributeToEndOfList (list: IAttributeList) =
        addAttributeToEndOfList "" list

    let makeMoveCaretToEmptyAttributeAction (list: IAttributeList) =
        Action<_>(fun (textControl: ITextControl) ->
                textControl.Caret.MoveTo(list.LastChild.GetDocumentStartOffset(), CaretVisualPlacement.Generic))

[<ContextAction(Group = "F#", Name = "Add empty attribute to type",
                Description = "Adds an empty attribute to type",
                Priority = 10s)>]
type AddEmptyAttributeToTypeAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(_, _) =
        let typeDecl = dataProvider.GetSelectedElement<IFSharpTypeOrExtensionDeclaration>()

        use writeCookie = WriteLockCookie.Create(typeDecl.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        typeDecl
        |> FSharpAttributesUtil.getTypeDeclarationAttributeList
        |> AttributeUtil.addEmptyAttributeToEndOfList
        |> AttributeUtil.makeMoveCaretToEmptyAttributeAction

    override this.IsAvailable(cache) =
        dataProvider.GetSelectedElement<IFSharpTypeOrExtensionDeclaration>()
        |> AttributeUtil.isAvailable dataProvider

    override this.Text = "Add empty attribute"

[<ContextAction(Group = "F#", Name = "Add empty attribute to binding",
                Description = "Adds an empty attribute to binding",
                Priority = 10s)>]
type AddEmptyAttributeToBindingAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(_, _) =
        let typeDecl = dataProvider.GetSelectedElement<ITopBinding>()

        use writeCookie = WriteLockCookie.Create(typeDecl.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        typeDecl
        |> FSharpAttributesUtil.getBindingAttributeList
        |> AttributeUtil.addEmptyAttributeToEndOfList
        |> AttributeUtil.makeMoveCaretToEmptyAttributeAction

    override this.IsAvailable(cache) =
        dataProvider.GetSelectedElement<ITopBinding>()
        |> AttributeUtil.isAvailable dataProvider

    override this.Text = "Add empty attribute"

[<ContextAction(Group = "F#", Name = "Add empty attribute to member",
                Description = "Adds an empty attribute to member",
                Priority = 10s)>]
type AddEmptyAttributeToMemberAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(_, _) =
        let memberDecl = dataProvider.GetSelectedElement<IMemberDeclaration>()

        use writeCookie = WriteLockCookie.Create(memberDecl.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        memberDecl
        |> FSharpAttributesUtil.getMemberDeclarationAttributeList
        |> AttributeUtil.addEmptyAttributeToEndOfList
        |> AttributeUtil.makeMoveCaretToEmptyAttributeAction

    override this.IsAvailable(cache) =
        dataProvider.GetSelectedElement<IMemberDeclaration>()
        |> AttributeUtil.isAvailable dataProvider

    override this.Text = "Add empty attribute"

[<ContextAction(Group = "F#", Name = "Add empty attribute to module",
                Description = "Adds an empty attribute to module",
                Priority = 10s)>]
type AddEmptyAttributeToModuleAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(_, _) =
        let moduleDecl = dataProvider.GetSelectedElement<IDeclaredModuleDeclaration>()

        use writeCookie = WriteLockCookie.Create(moduleDecl.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        moduleDecl
        |> FSharpAttributesUtil.getModuleDeclarationAttributeList
        |> AttributeUtil.addEmptyAttributeToEndOfList
        |> AttributeUtil.makeMoveCaretToEmptyAttributeAction

    override this.IsAvailable(cache) =
        dataProvider.GetSelectedElement<IDeclaredModuleDeclaration>()
        |> AttributeUtil.isAvailable dataProvider

    override this.Text = "Add empty attribute"

[<ContextAction(Group = "F#", Name = "Add empty attribute to parameter",
                Description = "Adds an attribute to parameter",
                Priority = 10s)>]
type AddEmptyAttributeToParameterAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(_, _) =
        let parameterDecl = dataProvider.GetSelectedElement<IParametersPatternDeclaration>()

        use writeCookie = WriteLockCookie.Create(parameterDecl.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let addParens =
            match parameterDecl.Parent with
            | :? IBinding ->
                not (parameterDecl.Pattern :? IParenPat)
            | :? IMemberDeclaration as memberDecl ->
                // not sure if this is totally working
                let pat = memberDecl.ParameterPatternsEnumerable.FirstOrDefault()
                isNotNull pat && not (pat :? IParenPat || pat.Parent :? IParenPat)
            | _ ->
                false

        if addParens then
            let factory = parameterDecl.CreateElementFactory()
            let pat = factory.CreateParenPat()
            pat.SetPattern(parameterDecl.Pattern) |> ignore
            ModificationUtil.ReplaceChild(parameterDecl.Pattern, pat) |> ignore

        parameterDecl
        |> FSharpAttributesUtil.getParameterDeclarationAttributeList
        |> AttributeUtil.addEmptyAttributeToEndOfList
        |> AttributeUtil.makeMoveCaretToEmptyAttributeAction

    override this.IsAvailable(cache) =
        dataProvider.GetSelectedElement<IParametersPatternDeclaration>()
        |> AttributeUtil.isAvailable dataProvider

    override this.Text = "Add attribute"