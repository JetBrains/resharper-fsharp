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
    let addEmptyAttributeToEndOfList (list: IAttributeList) =
        let factory = list.CreateElementFactory()
        let attribute = factory.CreateAttribute("Foo")
        ModificationUtil.DeleteChild(attribute.FSharpIdentifier) // strip foo because factory does not create empty attributes

        if list.AttributesEnumerable.IsEmpty() then
            FSharpAttributesUtil.addAttribute list attribute |> ignore
        else
            FSharpAttributesUtil.addAttributeAfter (list.AttributesEnumerable.LastOrDefault()) attribute

        list

[<ContextAction(Group = "F#", Name = "Add empty attribute to type", Priority = 1s,
                Description = "Adds an empty attribute to type")>]
type AddAttributeToTypeAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(var0) = failwith "todo"
    override this.IsAvailable(cache) = failwith "todo"
    override this.Text = "Add empty attribute"

[<ContextAction(Group = "F#", Name = "Add empty attribute to member", Priority = 1s,
                Description = "Adds an empty attribute to member")>]
type AddAttributeToMemberAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(var0) = failwith "todo"
    override this.IsAvailable(cache) = failwith "todo"
    override this.Text = "Add empty attribute"

[<ContextAction(Group = "F#", Name = "Add empty attribute to module", Priority = 1s,
                Description = "Adds an empty attribute to module")>]
type AddAttributeToModuleAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(_) = failwith "todo"
    override this.IsAvailable(cache) = failwith "todo"
    override this.Text = "Add empty attribute"

[<ContextAction(Group = "F#", Name = "Add empty attribute to parameter", Priority = 1s,
                Description = "Adds an attribute to parameter")>]
type AddAttributeToParameterAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(_, _) =

        let parameter = dataProvider.GetSelectedElement<IParametersPatternDeclaration>()

        use writeCookie = WriteLockCookie.Create(parameter.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let addParens =
            match parameter.Parent with
            | :? IBinding ->
                not (parameter.Pattern :? IParenPat)
            | :? IMemberDeclaration as memberDecl ->
                // not sure if this is totally working
                let pat = memberDecl.ParameterPatternsEnumerable.FirstOrDefault()
                isNotNull pat && not (pat :? IParenPat || pat.Parent :? IParenPat)
            | _ ->
                false

        if addParens then
            let factory = parameter.CreateElementFactory()
            let pat = factory.CreateParenPat()
            pat.SetPattern(parameter.Pattern) |> ignore
            ModificationUtil.ReplaceChild(parameter.Pattern, pat) |> ignore

        let attributeList =
            parameter
            |> FSharpAttributesUtil.getParameterDeclarationAttributeList
            |> AttributeUtil.addEmptyAttributeToEndOfList

        Action<_>(fun textControl ->
                textControl.Caret.MoveTo(attributeList.LastChild.GetDocumentStartOffset(), CaretVisualPlacement.Generic))


    override this.IsAvailable(cache) =
        dataProvider.GetSelectedElement<IParametersPatternDeclaration>()
        |> isNotNull

    override this.Text = "Add attribute"