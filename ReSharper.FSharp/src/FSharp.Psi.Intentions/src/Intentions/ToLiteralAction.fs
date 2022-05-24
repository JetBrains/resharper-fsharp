namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

module ToLiteralAction =
    let [<Literal>] Description = "Make value literal (const) by adding Literal attribute"

[<ContextAction(Name = "ToLiteral", Group = "F#", Description = ToLiteralAction.Description)>]
type ToLiteralAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let rec isSimplePattern (fsPattern: IFSharpPattern): bool =
        match fsPattern with
        | :? IReferencePat as refPat ->
            let referenceName = refPat.ReferenceName
            isNotNull referenceName && isNull referenceName.Qualifier

        | :? IAsPat as asPat -> isSimplePattern asPat.LeftPattern && isSimplePattern asPat.RightPattern
        | :? IParenPat as parenPat -> isSimplePattern parenPat.Pattern

        | _ -> false

    let hasLiteralAttribute (attrs: TreeNodeEnumerable<IAttribute>):bool =
        attrs |> Seq.exists (fun attr ->
            let referenceName = attr.ReferenceName
            isNotNull referenceName && referenceName.ShortName = "Literal")

    override x.Text = "To literal"

    override x.IsAvailable _ =
        // todo: allow on keyword
        // todo: check isMutable, isInline

        let binding = dataProvider.GetSelectedElement<ITopBinding>()
        let selectedRange = dataProvider.SelectedTreeRange
        if not (isValid binding && binding.GetNameRange().Contains(&selectedRange)) then false else

        if not (isSimplePattern binding.HeadPattern) then false else
        if hasLiteralAttribute binding.AttributesEnumerable then false else

        let expr = binding.Expression
        isNotNull expr && expr.IsConstantValue()

    override x.ExecutePsiTransaction(_, _) =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let attributeList = FSharpAttributesUtil.getBindingAttributeList binding
        let attribute = binding.CreateElementFactory().CreateAttribute("Literal")
        FSharpAttributesUtil.addAttribute attributeList attribute |> ignore

        null