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

    let getAttributeList (binding: IBinding): IAttributeList =
        let attributeLists = binding.AttributeLists
        if not attributeLists.IsEmpty then attributeLists.First() else

        FSharpAttributesUtil.addAttributesList false binding
        binding.AttributeLists.[0]

    let rec isSimplePattern (fsPattern: IFSharpPattern): bool =
        match fsPattern with
        | :? IReferencePat as refPat ->
            let referenceName = refPat.ReferenceName
            isNotNull referenceName && isNull referenceName.Qualifier

        | :? IAsPat as asPat -> isSimplePattern asPat.Pattern
        | :? IParenPat as parenPat -> isSimplePattern parenPat.Pattern

        | _ -> false

    let hasLiteralAttribute (attrs: TreeNodeEnumerable<IAttribute>):bool =
        attrs |> Seq.exists (fun attr ->
            let referenceName = attr.ReferenceName
            isNotNull referenceName && referenceName.ShortName = "Literal")

    let rec isLiteralBinding (binding: IBinding): bool =
        if hasLiteralAttribute binding.AttributesEnumerable then true else

        let letBindings = LetBindingsDeclarationNavigator.GetByBinding(binding)
        if isNull letBindings || letBindings.Bindings.[0] != binding then false else

        hasLiteralAttribute letBindings.AttributesEnumerable

    override x.Text = "To literal"

    override x.IsAvailable _ =
        // todo: allow on keyword
        // todo: check isMutable, isInline

        let binding = dataProvider.GetSelectedElement<ITopBinding>()
        let selectedRange = dataProvider.SelectedTreeRange
        if not (isValid binding && binding.GetNameRange().Contains(&selectedRange)) then false else

        if not (isSimplePattern binding.HeadPattern) then false else
        if isLiteralBinding binding then false else

        let expr = binding.Expression
        isNotNull expr && expr.IsConstantValue()

    override x.ExecutePsiTransaction(_, _) =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let attributeList = getAttributeList binding
        let attribute = binding.CreateElementFactory().CreateAttribute("Literal")
        FSharpAttributesUtil.addAttribute attributeList attribute

        null
