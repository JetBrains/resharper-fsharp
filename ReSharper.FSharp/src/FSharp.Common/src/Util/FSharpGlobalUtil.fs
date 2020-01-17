namespace global

[<AutoOpen>]
module FSharpGlobalUtil =
    type System.Object with
        member x.As() =
            match x with
            | :? 'T as t -> t
            | _ -> null

    /// Reference equality.
    let inline (==) a b = LanguagePrimitives.PhysicalEquality a b

    /// Reference inequality.
    let inline (!=) a b = not (a == b)

    let inline isNotNull x = not (isNull x)

    let someUnit = Some ()

    let (|IsNonNull|_|) value =
        if isNotNull value then Some value else None


[<AutoOpen>]
module FSharpGlobalAbbreviations =
    type Extension = System.Runtime.CompilerServices.ExtensionAttribute

    type ILogger = JetBrains.Util.ILogger
    type FileSystemPath = JetBrains.Util.FileSystemPath


[<AutoOpen>]
module IgnoreAll =
    type IgnoreAllBuilder() =
        member _.Yield _ = ()
        member _.Zero() = ()
        member _.Combine(_, _) = ()
        member _.Delay(f) = f ()

    let ignoreAll = IgnoreAllBuilder()


[<AutoOpen>]
module ProtocolSolutionExtensions =
    open JetBrains.ProjectModel
    open JetBrains.ReSharper.Host.Features
    open JetBrains.Rider.Model

    type ISolution with
        member x.RdFSharpModel =
            try x.GetProtocolSolution().GetRdFSharpModel()
            with _ -> null

    type RdFSharpModel with
        member x.EnableExperimentalFeaturesSafe =
            match x with
            | null -> false
            | fsModel -> fsModel.EnableExperimentalFeatures.Value
