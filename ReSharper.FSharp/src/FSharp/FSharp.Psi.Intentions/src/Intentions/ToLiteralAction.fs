namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

module ToLiteralAction =
    let [<Literal>] Description = "Make value literal (const) by adding Literal attribute"

[<ContextAction(Name = "ToLiteral", Group = "F#", Description = ToLiteralAction.Description)>]
[<ZoneMarker(typeof<ILanguageFSharpZone>, typeof<IProjectModelZone>, typeof<ITextControlsZone>, typeof<PsiFeaturesImplZone>)>]
type ToLiteralAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let getAttributeList (binding: IBinding): IAttributeList =
        let attributeLists = binding.AttributeLists
        if not attributeLists.IsEmpty then attributeLists.First() else

        FSharpAttributesUtil.addAttributeListToLetBinding false binding
        binding.AttributeLists[0]

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
        if not (isValid binding && binding.GetNameRange().Contains(dataProvider.SelectedTreeRange)) then false else

        if not (isSimplePattern binding.HeadPattern) then false else
        if hasLiteralAttribute binding.AttributesEnumerable then false else

        let expr = binding.Expression
        isNotNull expr && expr.IsConstantValue()

    override x.ExecutePsiTransaction(_, _) =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let attributeList = getAttributeList binding
        let attribute = binding.CreateElementFactory().CreateAttribute("Literal")
        FSharpAttributesUtil.addAttribute attributeList attribute |> ignore

        null
