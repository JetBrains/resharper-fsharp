namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util

open System
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Feature.Services.VisualElements
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Colors
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Backend.Env
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
[<ZoneMarker(typeof<IResharperHostCoreFeatureZone>, typeof<IRiderFeatureEnvironmentZone>)>]
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
        FSharpColorReference.Create(colorElement, referenceExpr, range)

    let getFromAppExpr (referenceExpr: IReferenceExpr) (qualifier: IReferenceExpr) : IColorReference =
        let reference = referenceExpr.Reference
        let name = reference.GetName()
        if not (name = "FromArgb" || name = "FromName") then null else

        let appExpr = PrefixAppExprNavigator.GetByFunctionExpression(referenceExpr)
        if isNull appExpr then null else

        let argExpression = appExpr.ArgumentExpression.IgnoreInnerParens().As<ITupleExpr>()
        if isNull argExpression then null else

        let args =
            argExpression.Expressions
            |> Seq.cast
            |> Seq.toArray

        let factory =
            Func<ITypeElement, IColorElement, IColorReference>(fun qualifierType color ->
                let range = Nullable(argExpression.GetDocumentRange())
                FSharpColorReference.Create(color, argExpression, range))

        PredefinedColorTypes.ColorReferenceFromInvocation(
            StringComparer.Ordinal, qualifier.Reference, reference, args, factory)

    interface IVisualElementFactory with
        member x.GetColorReference(node) =
            let referenceExpr = node.As<IReferenceExpr>()
            if isNull referenceExpr then null else

            let qualifier = referenceExpr.Qualifier.As<IReferenceExpr>()
            if isNull qualifier then null else

            let color = getFromProperty referenceExpr qualifier
            if isNotNull color then color else

            getFromAppExpr referenceExpr qualifier


and FSharpColorReference private (colorElement, owner, range) =
    interface IColorReference with
        member x.Owner = owner
        member x.ColorConstantRange = range
        member x.ColorElement = colorElement

        member x.Bind _ = ()
        member x.GetColorTable() = Seq.empty
        member x.BindOptions = ColorBindOptions(BindsToValue = false, BindsToName = false)

    static member Create(colorElement: IColorElement, owner: ITreeNode, range) =
        FSharpColorReference(colorElement, owner, range) :> IColorReference 