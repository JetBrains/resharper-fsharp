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
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.FSharpInjectionAnnotationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil
open JetBrains.ReSharper.Plugins.FSharp.Util

[<SolutionComponent>]
type FSharpRegexNodeProvider() =
    let rec evalOptionsArg (expr: IFSharpExpression) =
        match expr.IgnoreInnerParens() with
        //TODO: move to IBinaryExpr
        | :? IBinaryAppExpr as binaryAppExpr ->
            let left = evalOptionsArg binaryAppExpr.LeftArgument
            let right = evalOptionsArg binaryAppExpr.RightArgument
            if isPredefinedInfixOpApp "|||" binaryAppExpr then left ||| right
            elif isPredefinedInfixOpApp "&&&" binaryAppExpr then left &&& right
            elif isPredefinedInfixOpApp "^^^" binaryAppExpr then left ^^^ right
            else RegexOptions.None

        | expr when expr.ConstantValue <> ConstantValue.BAD_VALUE ->
            let constant = expr.ConstantValue
            let constantType = constant.Type.GetTypeElement()
            if isNotNull constantType && constantType.GetClrName() = RegExpPredefinedType.REGEX_OPTIONS_FQN then
                LanguagePrimitives.EnumOfValue (constant.ToIntUnchecked())
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
            regexPatternInfo ||

            let languageName = getAnnotationInfo<StringSyntaxAnnotationProvider, _>(attributesOwner)
            isNotNull languageName &&
            (equalsIgnoreCase StringSyntaxAnnotationProvider.Regex languageName ||
             equalsIgnoreCase InjectedLanguageIDs.ClrRegExpLanguage languageName)

        if not isSuccess then ValueNone else

        match expr |> getArgsOwner |> getOptionsArg with
        | Some arg -> ValueSome (evalOptionsArg arg)
        | None -> ValueSome RegexOptions.None

    let checkForRegexActivePattern (pat: ILiteralPat) =
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(pat.IgnoreParentParens())
        if isNull parametersOwnerPat || parametersOwnerPat.Identifier.GetText() <> "Regex" then ValueNone
        else ValueSome RegexOptions.None

    let checkForRegexTypeProvider (expr: IConstExpr) =
        let providedTypeName =
            ExprStaticConstantTypeUsageNavigator.GetByExpression(expr)
            |> PrefixAppTypeArgumentListNavigator.GetByTypeUsage
            |> TypeReferenceNameNavigator.GetByTypeArgumentList

        let isRegexProvider = isNotNull providedTypeName && providedTypeName.Identifier.GetSourceName() = "Regex"
        if isRegexProvider then ValueSome RegexOptions.None else ValueNone

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
        override _.Description = "Injects .NET Regular Expression in F# strings"
        override _.Guid = "7e4d8d57-335f-4692-9ff8-6b2fa003fb51"
        override _.Words = null
        override _.Attributes = [| RegexPatternAnnotationProvider.RegexPatternAttributeShortName
                                   StringSyntaxAnnotationProvider.StringSyntaxAttributeShortName |]
