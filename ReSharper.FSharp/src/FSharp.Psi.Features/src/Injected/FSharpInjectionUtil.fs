module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.FSharpInjectionAnnotationUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeAnnotations
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil

let getAnnotationInfo<'AnnotationProvider, 'TAnnotationInfo
when 'AnnotationProvider :> CodeAnnotationInfoProvider<IAttributesOwner, 'TAnnotationInfo>>
    (attributesOwner: IAttributesOwner) =
    attributesOwner
        .GetPsiServices()
        .GetCodeAnnotationsCache()
        .GetProvider<'AnnotationProvider>()
        .GetInfo(attributesOwner)

let getAttributesOwner (expr: IFSharpExpression) =
    match getArgsOwner expr with
    | NotNull _ ->
        let parameter = expr.As<IArgument>().MatchingParameter
        if isNull parameter then ValueNone else
        parameter.Element.As<IAttributesOwner>() |> ValueOption.ofObj
    | _ ->

    let declaration: IDeclaration =
        let typeMemberDecl = MemberDeclarationNavigator.GetByExpression(expr)
        if isNotNull typeMemberDecl then typeMemberDecl else
        BindingNavigator.GetByExpression(expr)

    if isNull declaration then ValueNone else
    declaration.DeclaredElement.As<IAttributesOwner>() |> ValueOption.ofObj
