module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.FSharpInjectionAnnotationUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.CodeAnnotations
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

let findAttributesOwner (expr: IFSharpExpression) attributeNames =
    let getAttributesOwner (declaredElement: IDeclaredElement) =
        match declaredElement with
        | :? IAccessor as accessor -> accessor.OwnerMember.As<IAttributesOwner>()
        | _ -> declaredElement.As<IAttributesOwner>()

    let psiServices = expr.GetPsiServices()

    let binaryExpr = BinaryAppExprNavigator.GetByRightArgument(expr)
    let isNamedArg = isNotNull binaryExpr && FSharpArgumentsUtil.IsNamedArgSyntactically(binaryExpr)
    let argCandidate: IFSharpExpression = if isNamedArg then binaryExpr else expr

    let argsOwner = getArgsOwner argCandidate
    let declaration: IFSharpDeclaration =
        if isNotNull argsOwner then null else

        let typeMemberDecl = MemberDeclarationNavigator.GetByExpression(expr)
        if isNotNull typeMemberDecl then typeMemberDecl else
        let topBinding = TopBindingNavigator.GetByExpression(expr)
        if isNull topBinding then null else topBinding.HeadPattern.As<IFSharpDeclaration>()

    let namedArgRefExpr = if isNamedArg then FSharpArgumentsUtil.TryGetNamedArgRefExpr(binaryExpr) else null

    let propertySetter =
        if isNamedArg then
            let propertyName = namedArgRefExpr.ShortName
            let hasAttributes =
                attributeNames
                |> Seq.exists (fun attributeName ->
                    psiServices.HasMemberWithAttribute(propertyName, attributeName))

            if hasAttributes then getAttributesOwner (namedArgRefExpr.Reference.Resolve().DeclaredElement)
            else null
        else null

    if isNotNull propertySetter then propertySetter else

    let memberName =
        if isNull argsOwner then
            if isNamedArg then null else
            if isNull declaration then null
            else declaration.SourceName

        else getReferenceName argsOwner

    if isNull memberName then null else

    let hasAttributes =
        attributeNames
        |> Seq.exists (fun x -> psiServices.HasMemberWithAttribute(memberName, x))

    if not hasAttributes then null else

    if isNamedArg then
        if isNull namedArgRefExpr then null else
        getAttributesOwner (namedArgRefExpr.Reference.Resolve().DeclaredElement)

    else
        if isNotNull argsOwner then
            let matchingParameter = getMatchingParameter expr
            if isNotNull matchingParameter then matchingParameter :> IAttributesOwner else null

        else
            getAttributesOwner declaration.DeclaredElement

let tryGetTypeProviderName (expr: IConstExpr) =
    let providedTypeName =
        ExprStaticConstantTypeUsageNavigator.GetByExpression(expr)
        |> PrefixAppTypeArgumentListNavigator.GetByTypeUsage
        |> TypeReferenceNameNavigator.GetByTypeArgumentList

    if isNotNull providedTypeName then ValueSome (providedTypeName.Identifier.GetSourceName())
    else ValueNone
