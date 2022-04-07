namespace global

[<AutoOpen>]
module FSharpGlobalUtil =
    type System.Object with
        member x.As<'T when 'T: not struct>() =
            match x with
            | :? 'T as t -> t
            | _ -> Unchecked.defaultof<_>

    /// Reference equality.
    let inline (==) a b = obj.ReferenceEquals(a, b)

    /// Reference inequality.
    let inline (!=) a b = not (a == b)

    let inline isNull x = x == null
    let inline isNotNull x = not (isNull x)

    let inline notNull<'T when 'T: not struct> (x: 'T) =
        JetBrains.Diagnostics.Assertion.NotNull(x)

    let inline isValid (node: ^T) =
        isNotNull node && (^T: (member IsValid: unit -> bool) node)

    let someUnit = Some ()

    let (|NotNull|_|) value =
        if isNotNull value then Some value else None


[<AutoOpen>]
module FSharpGlobalAbbreviations =
    type Extension = System.Runtime.CompilerServices.ExtensionAttribute

    type ILogger = JetBrains.Util.ILogger
    type FileSystemPath = JetBrains.Util.FileSystemPath
    type VirtualFileSystemPath = JetBrains.Util.VirtualFileSystemPath
    type InteractionContext = JetBrains.Util.InteractionContext    
    type FormatterHelper = JetBrains.ReSharper.Psi.Impl.CodeStyle.FormatterImplHelper

[<Extension;AutoOpen>]
module FSharpFileSystemPathExtensions =
    type JetBrains.Util.FileSystemPath with
        [<Extension>]
        member this.ToVirtualFileSystemPath() = JetBrains.Util.FileSystemPathExtensions.ToVirtualFileSystemPath(this)
        
    type JetBrains.Util.VirtualFileSystemPath with
        [<Extension>]
        member this.ToNativeFileSystemPath() = JetBrains.Util.VirtualFileSystemPathExtensions.ToNativeFileSystemPath(this)


[<AutoOpen>]
module IgnoreAll =
    type IgnoreAllBuilder() =
        member _.Yield _ = ()
        member _.Zero() = ()
        member _.Combine(_, _) = ()
        member _.Delay(f) = f ()

    let ignoreAll = IgnoreAllBuilder()


namespace JetBrains.ReSharper.Plugins.FSharp

open JetBrains.Annotations
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Features

[<AbstractClass; Sealed; Extension>]
type ProtocolSolutionExtensions =
    [<Extension; CanBeNull>]
    static member RdFSharpModel(solution: ISolution) =
        try solution.GetProtocolSolution().GetRdFSharpModel()
        with _ -> null
