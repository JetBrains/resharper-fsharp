namespace JetBrains.ReSharper.Plugins.FSharp

open System
open System.Runtime.CompilerServices
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

type ExperimentalFeature =
    | Formatter = 1
    | PostfixTemplates = 2
    | RedundantParenAnalysis = 3
    | AssemblyReaderShim = 4
    | ReSharperImportCompletion = 5

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
        match feature with
        | ExperimentalFeature.AssemblyReaderShim ->
            let fsOptions = solution.GetComponent<FSharpOptionsProvider>()
            fsOptions.NonFSharpProjectInMemoryAnalysis.Value

        | _ ->

        let experimentalFeatures = solution.GetComponent<FSharpExperimentalFeaturesProvider>()
        match feature with
        | ExperimentalFeature.Formatter -> experimentalFeatures.RedundantParensAnalysis.Value
        | ExperimentalFeature.PostfixTemplates -> experimentalFeatures.EnablePostfixTemplates.Value
        | ExperimentalFeature.RedundantParenAnalysis -> experimentalFeatures.RedundantParensAnalysis.Value
        | ExperimentalFeature.ReSharperImportCompletion -> experimentalFeatures.ReSharperImportCompletion.Value
        | _ -> failwith $"Unexpected feature: {feature}"

    [<Extension>]
    static member IsFSharpExperimentalFeatureEnabled(solution: ISolution, feature: ExperimentalFeature) =
        FSharpExperimentalFeatureCookie.IsEnabled(feature) || isEnabledInSettings solution feature

    [<Extension>]
    static member IsFSharpExperimentalFeatureEnabled(node: ITreeNode, feature: ExperimentalFeature) =
        let solution = node.GetSolution()
        FSharpExperimentalFeatures.IsFSharpExperimentalFeatureEnabled(solution, feature)
