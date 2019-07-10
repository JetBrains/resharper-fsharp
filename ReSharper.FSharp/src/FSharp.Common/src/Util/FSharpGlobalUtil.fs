namespace global

[<AutoOpen>]
module FSharpGlobalUtil =
    type System.Object with
        member x.As<'T when 'T: null>() =
            match x with
            | :? 'T as t -> t
            | _ -> null

    /// Reference equality.
    let inline (==) a b = LanguagePrimitives.PhysicalEquality a b

    /// Reference inequality.
    let inline (!=) a b = not (a == b)

    let inline isNotNull x = not (isNull x)

    let someUnit = Some ()

[<AutoOpen>]
module FSharpGlobalAbbreviations =
    type Extension = System.Runtime.CompilerServices.ExtensionAttribute

    type ILogger = JetBrains.Util.ILogger
    type FileSystemPath = JetBrains.Util.FileSystemPath
