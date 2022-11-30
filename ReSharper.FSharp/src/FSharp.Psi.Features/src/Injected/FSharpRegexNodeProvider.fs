namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open System.Text.RegularExpressions
open JetBrains.ProjectModel
open JetBrains.ReSharper.Features.RegExp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeAnnotations
open JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.FSharpInjectionAnnotationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Util

[<SolutionComponent>]
type FSharpRegexNodeProvider() =
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
        if isNull argumentsOwner then None else
        argumentsOwner.Arguments
        |> Seq.tryPick (fun arg ->
            match arg with
            | :? IFSharpExpression as fsharpArg ->
                let matchingParam = arg.MatchingParameter
                if isNull matchingParam then None else
                let element = matchingParam.Element
                if isNull element then None else
                match element.Type with
                | :? IDeclaredType as declType when
                    declType.GetClrName().Equals(RegExpPredefinedType.REGEX_OPTIONS_FQN) -> Some fsharpArg
                | _ -> None
            | _ -> None)

    let checkForAttributes (expr: IFSharpExpression) =
        match getAttributesOwner expr with
        | ValueNone -> ValueNone
        | ValueSome attributesOwner ->

        let isSuccess =
            let regexPatternInfo = getAnnotationInfo<RegexPatternAnnotationProvider, _>(attributesOwner)
            if regexPatternInfo then true else

            let languageName = getAnnotationInfo<StringSyntaxAnnotationProvider, _>(attributesOwner)
            equalsIgnoreCase StringSyntaxAnnotationProvider.Regex languageName ||
            equalsIgnoreCase InjectedLanguageIDs.ClrRegExpLanguage languageName

        if not isSuccess then ValueNone else

        match expr |> getArgsOwner |> getOptionsArg with
        | Some arg -> ValueSome (evalOptionsArg arg)
        | None -> ValueSome RegexOptions.None

    let checkForRegexActivePattern (pat: ILiteralPat) =
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(pat.IgnoreParentParens())
        if isNull parametersOwnerPat ||
           parametersOwnerPat.Identifier.GetText() <> "Regex" then ValueNone
        else ValueSome RegexOptions.None

    let checkForRegexTypeProvider (expr: IConstExpr) =
        ExprStaticConstantTypeUsageNavigator.GetByConstantExpression(expr)
        |> ValueOption.ofObj
        |> ValueOption.map PrefixAppTypeArgumentListNavigator.GetByTypeUsage
        |> ValueOption.map TypeReferenceNameNavigator.GetByTypeArgumentList
        |> ValueOption.bind (fun refName ->
            if refName.Identifier.GetText() = "Regex" then ValueSome RegexOptions.None else ValueNone)

    interface IInjectionNodeProvider with
        override _.Check(node, _, data) =
            data <- null

            let result =
                match node.Parent with
                | :? IInterpolatedStringExpr as expr -> checkForAttributes expr

                | :? ILiteralExpr as expr ->
                    let checkAttributesResult = checkForAttributes expr
                    if checkAttributesResult.IsSome then checkAttributesResult else
                    checkForRegexTypeProvider expr

                | :? ILiteralPat as pat -> checkForRegexActivePattern pat
                | _ -> ValueNone

            match result with
            | ValueNone -> false
            | ValueSome regexOptions ->

            data <- regexOptions
            true

        override _.GetPrefix(_, _) = null
        override _.GetSuffix(_, _) = null
        override _.SupportedOriginalLanguage = FSharpLanguage.Instance :> _
        override _.ProvidedLanguageID = InjectedLanguageIDs.ClrRegExpLanguage
        override _.Summary = ".NET Regular Expressions in F#"
        override _.Description = "Injects .NET Regular Expression in calls from F# code to Regex members"
        override _.Guid = "7e4d8d57-335f-4692-9ff8-6b2fa003fb51"
        override _.Words = [|"\""|] // any string
        override _.Attributes = [| RegexPatternAnnotationProvider.RegexPatternAttributeShortName
                                   StringSyntaxAnnotationProvider.StringSyntaxAttributeShortName |]
