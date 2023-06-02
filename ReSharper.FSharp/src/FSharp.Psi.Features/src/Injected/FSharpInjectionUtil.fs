module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.FSharpInjectionAnnotationUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeAnnotations
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

let getAnnotationInfo<'AnnotationProvider, 'TAnnotationInfo
when 'AnnotationProvider :> CodeAnnotationInfoProvider<IAttributesOwner, 'TAnnotationInfo>>
    (attributesOwner: IAttributesOwner) =
    attributesOwner
        .GetPsiServices()
        .GetCodeAnnotationsCache()
        .GetProvider<'AnnotationProvider>()
        .GetInfo(attributesOwner)

let getAttributesOwner (expr: IFSharpExpression) =
    match BinaryAppExprNavigator.GetByRightArgument(expr) with
    | binaryExpr when isNamedArgSyntactically binaryExpr ->
        let namedParam =
            match tryGetNamedArgRefExpr binaryExpr with
            | null -> null
            | namedRef -> namedRef.Reference.Resolve().DeclaredElement
        match namedParam with
        | :? IAttributesOwner as x -> x
        | _ -> null
    | _ ->

    let matchingParameter = getMatchingParameter expr
    if isNotNull matchingParameter then matchingParameter :> IAttributesOwner else

    let declaration: IDeclaration =
        let typeMemberDecl = MemberDeclarationNavigator.GetByExpression(expr)
        if isNotNull typeMemberDecl then typeMemberDecl else
        TopBindingNavigator.GetByExpression(expr)

    if isNull declaration then null else declaration.DeclaredElement.As<IAttributesOwner>()

let tryGetTypeProviderName (expr: IConstExpr) =
    let providedTypeName =
        ExprStaticConstantTypeUsageNavigator.GetByExpression(expr)
        |> PrefixAppTypeArgumentListNavigator.GetByTypeUsage
        |> TypeReferenceNameNavigator.GetByTypeArgumentList

    if isNotNull providedTypeName then ValueSome (providedTypeName.Identifier.GetSourceName())
    else ValueNone
