namespace global

open System
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features
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
    [<Extension>]
    static member RdFSharpModel(solution: ISolution) =
        try solution.GetProtocolSolution().GetRdFSharpModel()
        with _ -> null

    [<Extension>]
    static member EnableExperimentalFeaturesSafe(rdFSharpModel: RdFSharpModel) =
        if FSharpRegistryUtil.AllowExperimentalFeaturesCookie.Enabled then true else

        match rdFSharpModel with
        | null -> false
        | fsModel -> fsModel.EnableExperimentalFeatures.Value
