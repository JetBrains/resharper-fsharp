namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.CodeAnnotations
open JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.RegexPatternDetector

[<SolutionComponent>]
type FSharpRegexNodeProvider() =
    let attributes = [| RegexPatternAnnotationProvider.RegexPatternAttributeShortName |]

    interface IInjectionNodeProvider with
        override _.Check(node, _, data) =
            data <- null

            let result =
                //TODO: Parent?
                match node.Parent with
                | :? IInterpolatedStringExpr as expr -> isRegularExpressionPattern expr
                | :? ILiteralExpr as expr -> isRegularExpressionPattern expr
                //| :? ILiteralPat as pat -> checkLiteralPat pat
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
        override _.Attributes = attributes
