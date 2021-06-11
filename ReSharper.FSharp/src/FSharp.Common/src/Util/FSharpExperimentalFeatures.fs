namespace JetBrains.ReSharper.Plugins.FSharp

open System
open System.Runtime.CompilerServices
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<RequireQualifiedAccess>]
type ExperimentalFeature =
    | Formatter
    | PostfixTemplates
    | RedundantParenAnalysis

type FSharpExperimentalFeatureCookie(feature: ExperimentalFeature) =
    static let cookies = OneToListMap<ExperimentalFeature, IDisposable>()

    static member Create(feature: ExperimentalFeature) =
        let cookie = new FSharpExperimentalFeatureCookie(feature)
        cookies.Add(feature, cookie)
        cookie

    static member IsEnabled(feature: ExperimentalFeature) =
        cookies.ContainsKey(feature)

    interface IDisposable with
        member this.Dispose() =
            cookies.Remove(feature, this) |> ignore

[<AbstractClass; Sealed; Extension>]
type FSharpExperimentalFeatures() =
    static let isEnabledInSettings (solution: ISolution) feature =
        let provider = solution.GetComponent<FSharpExperimentalFeaturesProvider>()

        match feature with
        | ExperimentalFeature.Formatter -> provider.RedundantParensAnalysis.Value
        | ExperimentalFeature.PostfixTemplates -> provider.EnablePostfixTemplates.Value
        | ExperimentalFeature.RedundantParenAnalysis -> provider.RedundantParensAnalysis.Value

    [<Extension>]
    static member IsFSharpExperimentalFeatureEnabled(solution: ISolution, feature: ExperimentalFeature) =
        FSharpExperimentalFeatureCookie.IsEnabled(feature) || isEnabledInSettings solution feature

    [<Extension>]
    static member IsFSharpExperimentalFeatureEnabled(node: ITreeNode, feature: ExperimentalFeature) =
        let solution = node.GetSolution()
        FSharpExperimentalFeatureCookie.IsEnabled(feature) || isEnabledInSettings solution feature
