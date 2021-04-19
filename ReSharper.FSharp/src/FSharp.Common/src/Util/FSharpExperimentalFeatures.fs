namespace global

open System
open System.Collections.Generic
open JetBrains.Annotations
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Core.Features
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Settings

module FSharpExperimentalFeatures =
    [<AbstractClass>]
    type EnableFeatureCookieBase<'T when 'T :> EnableFeatureCookieBase<'T> and 'T: (new: unit -> 'T)>() =
        static let cookies = Stack()

        static member Create() =
            let t = new 'T()
            cookies.Push(t)
            t

        static member Enabled = cookies.Count > 0

        interface IDisposable with
            member this.Dispose() =
                cookies.Pop() |> ignore

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
