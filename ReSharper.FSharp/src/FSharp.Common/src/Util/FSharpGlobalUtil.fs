namespace global

[<AutoOpen>]
module FSharpGlobalUtil =
    type System.Object with
        member x.As() =
            match x with
            | :? 'T as t -> t
            | _ -> null

    /// Reference equality.
    let inline (==) a b = obj.ReferenceEquals(a, b)

    /// Reference inequality.
    let inline (!=) a b = not (a == b)

    let inline isNull x = x == null
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
