namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util

open System
open JetBrains.ReSharper.Feature.Services.VisualElements
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Colors
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpVisualElementFactory() =
    let getFromProperty (referenceExpr: IReferenceExpr) (qualifier: IReferenceExpr): IColorReference =
        let name = referenceExpr.Reference.GetName()
        let color = ColorParsing.GetNamedColor(name)
        if not color.HasValue then null else

        let qualifierType = qualifier.Reference.Resolve().DeclaredElement.As<ITypeElement>()
        if isNull qualifierType then null else

        let colorTypes = PredefinedColorTypes.Get(referenceExpr.GetPsiModule())
        if not (colorTypes.HasPredefinedColorMembers(qualifierType)) then null else

        let property = referenceExpr.Reference.Resolve().DeclaredElement.As<IProperty>()
        if isNull property then null else

        let colorElement = ColorElement(color.Value, name)
        let range = Nullable(referenceExpr.Identifier.GetDocumentRange())
        FSharpColorReference(colorElement, referenceExpr, range) :> _

    interface IVisualElementFactory with
        member x.GetColorReference(node) =
            let referenceExpr = node.As<IReferenceExpr>()
            if isNull referenceExpr then null else

            let qualifier = referenceExpr.Qualifier.As<IReferenceExpr>()
            if isNull qualifier then null else

            getFromProperty referenceExpr qualifier


and FSharpColorReference(colorElement: IColorElement, owner: ITreeNode, range) =
    interface IColorReference with
        member x.Owner = owner
        member x.ColorConstantRange = range
        member x.ColorElement = colorElement

        member x.Bind _ = ()
        member x.GetColorTable() = Seq.empty
        member x.BindOptions = ColorBindOptions(BindsToValue = false, BindsToName = false)
