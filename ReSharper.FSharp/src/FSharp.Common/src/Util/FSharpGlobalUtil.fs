namespace global

[<AutoOpen>]
module FSharpGlobalUtil =
    type System.Object with
        member x.As<'T when 'T: not struct>() =
            match x with
            | :? 'T as t -> t
            | _ -> Unchecked.defaultof<_>

    /// Reference equality.
    let inline (==) (a: 'A when 'A: not struct) (b: 'B when 'B: not struct) =
        obj.ReferenceEquals(a, b)

    /// Reference inequality.
    let inline (!=) a b = not (a == b)

    let inline isNull<'T when 'T: not struct> (x: 'T) =
        match box x with
        | null -> true
        | _ -> false

    let inline isNotNull<'T when 'T: not struct> (x: 'T) =
        match box x with
        | null -> false
        | _ -> true

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

[<Extension; AutoOpen>]
module FSharpFileSystemPathExtensions =
    open JetBrains.Util

    type FileSystemPath with
        [<Extension>]
        member this.ToVirtualFileSystemPath() =
            FileSystemPathExtensions.ToVirtualFileSystemPath(this)
        
    type VirtualFileSystemPath with
        [<Extension>]
        member this.ToNativeFileSystemPath() =
            VirtualFileSystemPathExtensions.ToNativeFileSystemPath(this)

    type ValueOption<'a> with
        static member OfOption(option) =
            match option with
            | Some x -> ValueSome x
            | None -> ValueNone

[<Extension; AutoOpen>]
module DocumentRangeExtensions =
    open JetBrains.DocumentModel
    open JetBrains.ReSharper.Psi

    type DocumentRange with
        [<Extension>]
        member inline this.Contains(documentOffset: DocumentOffset) =
            this.Contains(&documentOffset)

        [<Extension>]
        member inline this.Contains(documentRange: DocumentRange) =
            this.Contains(&documentRange)

    type TreeTextRange with
        [<Extension>]
        member inline this.Contains(treeTextRange: TreeTextRange) =
            this.Contains(&treeTextRange)

[<AutoOpen>]
module IgnoreAll =
    type IgnoreAllBuilder() =
        member _.Yield _ = ()
        member _.Zero() = ()
        member _.Combine(_, _) = ()
        member _.Delay(f) = f ()

    let ignoreAll = IgnoreAllBuilder()


namespace JetBrains.ReSharper.Resources.Shell

type ReadLockCookie = NonCSharpInteropReadLockCookie


namespace JetBrains.ReSharper.Plugins.FSharp

open JetBrains.Annotations
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Protocol

[<AbstractClass; Sealed; Extension>]
type ProtocolSolutionExtensions =
    [<Extension; CanBeNull>]
    static member RdFSharpModel(solution: ISolution) =
        try solution.GetProtocolSolution().GetRdFSharpModel()
        with _ -> null
