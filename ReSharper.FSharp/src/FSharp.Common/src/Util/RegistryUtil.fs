namespace global

open System
open JetBrains.Annotations
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Model

module FSharpRegistryUtil =
    type AllowExperimentalFeaturesCookie() =
        static let mutable enabled = false

        static member Create() =
            enabled <- true
            new AllowExperimentalFeaturesCookie()

        static member Enabled = enabled

        interface IDisposable with
            member _.Dispose() =
                enabled <- false


[<AbstractClass; Sealed; Extension>]
type ProtocolSolutionExtensions =
    [<Extension; CanBeNull>]
    static member RdFSharpModel(solution: ISolution) =
        try solution.GetProtocolSolution().GetRdFSharpModel()
        with _ -> null

[<AbstractClass; Sealed; Extension>]
type FSharpExperimentalFeaturesEx =
    [<Extension>]
    static member FSharpExperimentalFeaturesEnabled(solution: ISolution) =
        if FSharpRegistryUtil.AllowExperimentalFeaturesCookie.Enabled then true else

        match solution.RdFSharpModel() with
        | null -> false
        | fsModel -> fsModel.EnableExperimentalFeatures.Value

    [<Extension>]
    static member FSharpExperimentalFeaturesEnabled(node: ITreeNode) =
        FSharpExperimentalFeaturesEx.FSharpExperimentalFeaturesEnabled(node.GetSolution())
