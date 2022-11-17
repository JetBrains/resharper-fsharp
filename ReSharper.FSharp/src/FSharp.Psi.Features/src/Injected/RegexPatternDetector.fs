module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.RegexPatternDetector

open System.Text.RegularExpressions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.FSharpInjectionAnnotationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Features.RegExp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi.CodeAnnotations
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil

let rec evalOptionsArg (expr: IFSharpExpression) =
    match expr.IgnoreInnerParens() with
    | :? IReferenceExpr as refExpr ->
        match refExpr.Reference.Resolve().DeclaredElement with
        | :? IField as field when field.IsEnumMember || field.IsConstant ->
            let typeElement = field.Type.GetTypeElement()
            if isNotNull typeElement && typeElement.GetClrName() = RegExpPredefinedType.REGEX_OPTIONS_FQN then
                let value = field.ConstantValue.Value :?> RegexOptions
                value
            else
                RegexOptions.None
        | _ -> RegexOptions.None

    | :? IBinaryAppExpr as binaryAppExpr ->
        let left = evalOptionsArg binaryAppExpr.LeftArgument
        let right = evalOptionsArg binaryAppExpr.RightArgument

        if isPredefinedInfixOpApp "|||" binaryAppExpr then left ||| right
        elif isPredefinedInfixOpApp "&&&" binaryAppExpr then left &&& right
        elif isPredefinedInfixOpApp "^^^" binaryAppExpr then left ^^^ right
        else RegexOptions.None

    | _ -> RegexOptions.None

let getOptionsArg (argumentsOwner: IFSharpArgumentsOwner) =
    argumentsOwner.Arguments
    |> Seq.tryPick (fun arg ->
        match arg with
        | :? IFSharpExpression as fsharpArg ->
            match arg.MatchingParameter with
            | null -> None
            | matchingParam ->
                let element = matchingParam.Element
                if isNull element then None else
                match element.Type with
                | :? IDeclaredType as declType when
                    declType.GetClrName().Equals(RegExpPredefinedType.REGEX_OPTIONS_FQN) -> Some fsharpArg
                | _ -> None
        | _ -> None)

let hasAnnotationInfo (expr: IFSharpExpression) =
    let argumentsOwner = getArgsOwner expr
    if isNotNull argumentsOwner then
        let parameter = expr.As<IArgument>().MatchingParameter
        if isNull parameter then ValueNone else
        let annotationInfo = getAnnotationInfo<RegexPatternAnnotationProvider, _>(parameter.Element)
        if annotationInfo then ValueSome(argumentsOwner) else ValueNone
    else

    let chameleonExpr = ChameleonExpressionNavigator.GetByExpression(expr)
    let memberDecl: IDeclaration =
        let typeMemberDecl = MemberDeclarationNavigator.GetByChameleonExpression(chameleonExpr)
        if isNotNull typeMemberDecl then typeMemberDecl else
        TopBindingNavigator.GetByChameleonExpression(chameleonExpr)

    if isNull memberDecl then ValueNone else
    //TODO: prefomance?
    let attributesOwner = memberDecl.DeclaredElement.As<IAttributesOwner>()
    if isNull attributesOwner then ValueNone else
    let annotationInfo = getAnnotationInfo<RegexPatternAnnotationProvider, _>(attributesOwner)
    if annotationInfo then ValueSome(null) else ValueNone

let isRegularExpressionPattern (expr: IFSharpExpression) =
    match hasAnnotationInfo expr with
    | ValueNone ->
        // Regex<pattern> type provider
        expr.Parent.As<IExprStaticConstantTypeUsage>()//ExprStaticConstantTypeUsageNavigator.GetByExpression(expr)
        |> ValueOption.ofObj
        |> ValueOption.map PrefixAppTypeArgumentListNavigator.GetByTypeUsage
        |> ValueOption.map TypeReferenceNameNavigator.GetByTypeArgumentList
        |> ValueOption.bind (fun refName ->
            if refName.Identifier.GetText() = "Regex" then ValueSome RegexOptions.None else ValueNone)

    | ValueSome null -> ValueSome RegexOptions.None
    | ValueSome argumentsOwner ->

    match getOptionsArg argumentsOwner with
    | Some arg -> evalOptionsArg arg |> ValueSome
    | None -> ValueSome RegexOptions.None
