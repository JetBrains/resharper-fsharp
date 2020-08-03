namespace global

open System
open System.Collections.Generic
open JetBrains.Annotations
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Model

module FSharpRegistryUtil =
    [<AbstractClass>]
    type EnabledCookieBase<'T when 'T :> EnabledCookieBase<'T> and 'T : (new : unit -> 'T)>() =
        static let enabled = Stack [false]

        static member Create() =
            enabled.Push(true)
            new 'T()

        static member Enabled = enabled.Peek()

        interface IDisposable with
            member _.Dispose() =
                enabled.Pop() |> ignore

    type AllowExperimentalFeaturesCookie() = inherit EnabledCookieBase<AllowExperimentalFeaturesCookie>()
    type AllowFormatterCookie() = inherit EnabledCookieBase<AllowFormatterCookie>()

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

    [<Extension>]
    static member FSharpFormatterEnabled(solution: ISolution) =
        if FSharpRegistryUtil.AllowFormatterCookie.Enabled then true else

        match solution.RdFSharpModel() with
        | null -> false
        | fsModel -> fsModel.EnableFormatter.Value

    [<Extension>]
    static member FSharpFormatterEnabled(node: ITreeNode) =
        FSharpExperimentalFeaturesEx.FSharpFormatterEnabled(node.GetSolution())
