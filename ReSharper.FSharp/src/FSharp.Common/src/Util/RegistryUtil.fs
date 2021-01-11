namespace global

open System
open System.Collections.Generic
open JetBrains.Annotations
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Settings

module FSharpExperimentalFeatures =
    [<AbstractClass>]
    type EnableFeatureCookieBase<'T when 'T :> EnableFeatureCookieBase<'T> and 'T: (new: unit -> 'T)>() =
        static let enabled = Stack [false]

        static member Create() =
            enabled.Push(true)
            new 'T()

        static member Enabled = enabled.Peek()

        interface IDisposable with
            member _.Dispose() =
                enabled.Pop() |> ignore

    type EnableInlineVarRefactoringCookie() = inherit EnableFeatureCookieBase<EnableInlineVarRefactoringCookie>()
    type EnableRedundantParenAnalysisCookie() = inherit EnableFeatureCookieBase<EnableRedundantParenAnalysisCookie>()
    type EnableFormatterCookie() = inherit EnableFeatureCookieBase<EnableFormatterCookie>()


[<AbstractClass; Sealed; Extension>]
type ProtocolSolutionExtensions =
    [<Extension; CanBeNull>]
    static member RdFSharpModel(solution: ISolution) =
        try solution.GetProtocolSolution().GetRdFSharpModel()
        with _ -> null


[<AbstractClass; Sealed; Extension>]
type FSharpExperimentalFeaturesEx =
    [<Extension>]
    static member FSharpInlineVarRefactoringEnabled(solution: ISolution) =
        if FSharpExperimentalFeatures.EnableInlineVarRefactoringCookie.Enabled then true else

        let settingsProvider = solution.GetComponent<FSharpExperimentalFeaturesProvider>()
        settingsProvider.EnableInlineVarRefactoring.Value

    [<Extension>]
    static member FSharpPostfixTemplatesEnabled(solution: ISolution) =
        let settingsProvider = solution.GetComponent<FSharpExperimentalFeaturesProvider>()
        settingsProvider.EnablePostfixTemplates.Value

    [<Extension>]
    static member FSharpRedundantParenAnalysisEnabled(solution: ISolution) =
        if FSharpExperimentalFeatures.EnableRedundantParenAnalysisCookie.Enabled then true else

        let settingsProvider = solution.GetComponent<FSharpExperimentalFeaturesProvider>()
        settingsProvider.RedundantParensAnalysis.Value

    [<Extension>]
    static member FSharpFormatterEnabled(solution: ISolution) =
        if FSharpExperimentalFeatures.EnableFormatterCookie.Enabled then true else

        let settingsProvider = solution.GetComponent<FSharpExperimentalFeaturesProvider>()
        settingsProvider.Formatter.Value

    [<Extension>]
    static member FSharpFormatterEnabled(node: ITreeNode) =
        FSharpExperimentalFeaturesEx.FSharpFormatterEnabled(node.GetSolution())
