module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.FSharpInjectionAnnotationUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.CodeAnnotations
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

let getAttributesOwner (expr: IFSharpExpression) attributeNames =
    let psiServices = expr.GetPsiServices()

    let binaryExpr = BinaryAppExprNavigator.GetByRightArgument(expr)
    let isNamedArg = isNotNull binaryExpr && isNamedArgSyntactically binaryExpr
    let argCandidate: IFSharpExpression = if isNamedArg then binaryExpr else expr

    let argsOwner = getArgsOwner argCandidate
    let declaration: IFSharpDeclaration =
        if isNotNull argsOwner then null else

        let typeMemberDecl = MemberDeclarationNavigator.GetByExpression(expr)
        if isNotNull typeMemberDecl then typeMemberDecl else
        let topBinding = TopBindingNavigator.GetByExpression(expr)
        if isNull topBinding then null else topBinding.HeadPattern.As<IFSharpDeclaration>()

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
        let namedRef = tryGetNamedArgRefExpr binaryExpr
        if isNull namedRef then null else
        namedRef.Reference.Resolve().DeclaredElement.As<IAttributesOwner>()

    else
        if isNotNull argsOwner then
            let matchingParameter = getMatchingParameter expr
            if isNotNull matchingParameter then matchingParameter :> IAttributesOwner else null

        else
            declaration.DeclaredElement.As<IAttributesOwner>()

let tryGetTypeProviderName (expr: IConstExpr) =
    let providedTypeName =
        ExprStaticConstantTypeUsageNavigator.GetByExpression(expr)
        |> PrefixAppTypeArgumentListNavigator.GetByTypeUsage
        |> TypeReferenceNameNavigator.GetByTypeArgumentList

    if isNotNull providedTypeName then ValueSome (providedTypeName.Identifier.GetSourceName())
    else ValueNone
