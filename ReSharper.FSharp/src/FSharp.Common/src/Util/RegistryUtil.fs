namespace global

open System
open JetBrains.Annotations
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Model

module FSharpRegistryUtil =
    [<AbstractClass>]
    type EnabledCookieBase<'T when 'T :> EnabledCookieBase<'T> and 'T : (new : unit -> 'T)>() =
        static let mutable enabled = false

        static member Create() =
            enabled <- true
            new 'T()

        static member Enabled = enabled

        interface IDisposable with
            member _.Dispose() =
                enabled <- false

    type AllowExperimentalFeaturesCookie() = inherit EnabledCookieBase<AllowExperimentalFeaturesCookie>()
    type AllowFormatterCookie() = inherit EnabledCookieBase<AllowFormatterCookie>()

[<AbstractClass; Sealed; Extension>]
type ProtocolSolutionExtensions =
    [<Extension; CanBeNull>]
    static member RdFSharpModel(solution: ISolution) =
        try solution.GetProtocolSolution().GetRdFSharpModel()
        with _ -> null

[<AbstractClass; Sealed; Extension>]
type FSharpExperimentalFeaturesEx() =
    static let getFsModelFlagIfNotEnabled property enabled (solution : ISolution) =
        if enabled then true else

        match solution.RdFSharpModel() with
        | null -> false
        | fsModel -> property fsModel
        
    [<Extension>]
    static member FSharpExperimentalFeaturesEnabled(solution: ISolution) =
        getFsModelFlagIfNotEnabled (fun fsModel -> fsModel.EnableExperimentalFeatures.Value)
            FSharpRegistryUtil.AllowExperimentalFeaturesCookie.Enabled
            solution

    [<Extension>]
    static member FSharpExperimentalFeaturesEnabled(node: ITreeNode) =
        FSharpExperimentalFeaturesEx.FSharpExperimentalFeaturesEnabled(node.GetSolution())

    [<Extension>]
    static member FSharpFormatterEnabled(solution: ISolution) =
        getFsModelFlagIfNotEnabled (fun fsModel -> fsModel.EnableFormatter.Value)
            FSharpRegistryUtil.AllowFormatterCookie.Enabled
            solution

    [<Extension>]
    static member FSharpFormatterEnabled(node: ITreeNode) =
        FSharpExperimentalFeaturesEx.FSharpFormatterEnabled(node.GetSolution())
