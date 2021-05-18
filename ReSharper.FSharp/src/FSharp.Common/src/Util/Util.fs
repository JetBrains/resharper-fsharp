namespace JetBrains.ReSharper.Plugins.FSharp.Util

open FSharp.Compiler.Diagnostics
open FSharp.Compiler.Syntax
open FSharp.Compiler.Xml
open JetBrains.Annotations
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Threading

[<AutoOpen>]
module rec CommonUtil =
    open System
    open System.Collections.Generic
    open System.Diagnostics
    open System.Text.RegularExpressions
    open FSharp.Compiler.Text
    open JetBrains.Application.UI.Icons.ComposedIcons
    open JetBrains.DataFlow
    open JetBrains.DocumentModel
    open JetBrains.Lifetimes
    open JetBrains.Util
    open JetBrains.Util.dataStructures.TypedIntrinsics

    let ensureAbsolute (path: FileSystemPath) (projectDirectory: FileSystemPath) =
        match path.AsRelative() with
        | null -> path
        | relativePath -> projectDirectory.Combine(relativePath)

    let concatErrors errors =
        Seq.fold (fun s (e: FSharpDiagnostic) -> s + "\n" + e.Message) "" errors

    let logErrors (logger: ILogger) message errors =
        logger.Warn("{0}: {1}", message, concatErrors errors)

    [<CompiledName("DecompileOpName")>]
    let decompileOpName name =
        PrettyNaming.DecompileOpName name

    type IDictionary<'TKey, 'TValue> with
        member x.remove (key: 'TKey) = x.Remove key |> ignore
        member x.add (key: 'TKey, value: 'TValue) = x.Add(key, value)
        member x.contains (key: 'TKey) = x.ContainsKey key

    type ISet<'T> with
        member x.remove el = x.Remove el |> ignore
        member x.add el = x.Add el |> ignore

    let fsExtensions = ["fs"; "fsi"; "ml"; "mli"; "fsx"; "fsscript"] |> Set.ofList
    let dllExtensions = ["dll"; "exe"] |> Set.ofList

    let isImplFileExtension extension =
        extension = "fs" || extension = "ml"

    let isSigFileExtension extension =
        extension = "fsi" || extension = "mli"

    let (|ImplExtension|_|) extension =
        if isImplFileExtension extension then someUnit else None

    let (|SigExtension|_|) extension =
        if isSigFileExtension extension then someUnit else None

    let isImplFile (path: FileSystemPath) =
        isImplFileExtension path.ExtensionNoDot

    let isSigFile (path: FileSystemPath) =
        isSigFileExtension path.ExtensionNoDot

    let getFullPath (path: FileSystemPath) =
        path.FullPath

    type Line = Int32<DocLine>
    type Column = Int32<DocColumn>

    type Int32<'T> with
        [<DebuggerStepThrough>]
        member x.Next = x.Plus1()

        [<DebuggerStepThrough>]
        member x.Previous = x.Minus1()

    let inline docLine (x: int)   = Line.op_Explicit(x)
    let inline docColumn (x: int) = Column.op_Explicit(x)
    let inline docCoords line column = DocumentCoords(docLine (line - 1), docColumn column)

    type Range with
        member inline x.GetStartLine()   = x.StartLine - 1 |> docLine
        member inline x.GetEndLine()     = x.EndLine - 1   |> docLine

    let tryGetValue (key: 'TKey) (dictionary: IDictionary<'TKey,'TValue>) =
        let res = ref Unchecked.defaultof<'TValue>
        match dictionary.TryGetValue(key, res), res with
        | true, value -> Some !value
        | _ -> None

    let inline (|ArgValue|) (arg: PropertyChangedEventArgs<_>) = arg.New

    let inline (|AddRemoveArgs|) (args: AddRemoveEventArgs<_>) =
        args.Value

    let inline (|Pair|) (pair: Pair<_,_>) =
        pair.First, pair.Second

    let inline (|PsiModuleReference|) (ref: IPsiModuleReference) =
        ref.Module

    let (|ProjectFile|ProjectFolder|UnknownProjectItem|) (projectItem: IProjectItem) =
        match projectItem with
        | :? IProjectFile as file -> ProjectFile file
        | :? IProjectFolder as folder -> ProjectFolder folder
        | _ -> UnknownProjectItem

    let inline (|AsList|) seq = List.ofSeq seq

    let (|OperationCanceled|_|) (exn: Exception) =
        if exn.IsOperationCanceled() then someUnit else None

    let equalsIgnoreCase other (string: string) =
        string.Equals(other, StringComparison.OrdinalIgnoreCase)

    let (|IgnoreCase|_|) other string =
        if equalsIgnoreCase other string then someUnit else None

    let startsWith other (string: string) =
        string.StartsWith(other, StringComparison.Ordinal)

    let endsWith other (string: string) =
        string.EndsWith(other, StringComparison.Ordinal)

    let getCommonParent path1 path2 =
        FileSystemPath.GetDeepestCommonParent(path1, path2)

    /// Used in tests. Should not be invoked on BackSlashSeparatedRelativePath.
    let (|UnixSeparators|) (path: IPath) =
        let separatorStyle = FileSystemPathEx.SeparatorStyle.Unix
        match path with
        | :? FileSystemPath as path -> path.NormalizeSeparators(separatorStyle)
        | :? RelativePath as path -> path.NormalizeSeparators(separatorStyle)
        | _ -> failwith "Should not be invoked on BackSlashSeparatedRelativePath."

    let setComparer =
        { new IEqualityComparer<HashSet<_>> with
            member this.Equals(x, y) = x.SetEquals(y)
            member this.GetHashCode(x) = x.Count }

    type Lifetime with
        member x.AddAction2(func: Func<_,_>) =
            x.OnTermination(fun _ -> func.Invoke() |> ignore) |> ignore

    let compose a b = CompositeIconId.Compose(a, b)

    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None

[<AutoOpen>]
module rec FcsUtil =
    let inline (|ExprRange|) (expr: SynExpr) = expr.Range
    let inline (|PatRange|) (pat: SynPat) = pat.Range
    let inline (|IdentRange|) (id: Ident) = id.idRange
    let inline (|TypeRange|) (typ: SynType) = typ.Range

    let inline (|IdentText|_|) text (id: Ident) =
        if id.idText = text then someUnit else None

    let inline (|LongIdentLid|) (lid: LongIdentWithDots) = lid.Lid

    let (|XmlDoc|) (preXmlDoc: PreXmlDoc) =
        preXmlDoc.ToXmlDoc(false, None)


[<AutoOpen>]
module rec FSharpMsBuildUtils =
    open BuildActions
    open ItemTypes
    open JetBrains.Platform.MsBuildHost.Models

    module ItemTypes =
        let [<Literal>] compileBeforeItemType = "CompileBefore"
        let [<Literal>] compileAfterItemType = "CompileAfter"
        let [<Literal>] folderItemType = "Folder"

    module BuildActions =
        let compileBefore = BuildAction.GetOrCreate(compileBeforeItemType)  
        let compileAfter = BuildAction.GetOrCreate(compileAfterItemType)  

    let isCompileBefore itemType =
        equalsIgnoreCase compileBeforeItemType itemType

    let isCompileAfter itemType =
        equalsIgnoreCase compileAfterItemType itemType

    let (|CompileBefore|_|) itemType =
        if isCompileBefore itemType then someUnit else None

    let (|CompileAfter|_|) itemType =
        if isCompileAfter itemType then someUnit else None

    let (|Folder|_|) (itemType: string) =
        if equalsIgnoreCase folderItemType itemType then someUnit else None

    let (|BuildAction|) itemType =
        BuildAction.GetOrCreate(itemType)

    let (|RdItem|) (item: RdProjectItem) =
        RdItem (item.ItemType, item.EvaluatedInclude)

    let (|SourceFile|_|) (buildAction: BuildAction) =
        if buildAction.IsCompile() || buildAction = compileBefore || buildAction = compileAfter then someUnit else None

    let (|Resource|_|) buildAction =
        if buildAction = BuildAction.RESOURCE then someUnit else None

    let changesOrder = function
        | CompileBefore | CompileAfter -> true
        | _ -> false

    type BuildAction with
        member x.ChangesOrder = changesOrder x.Value

[<Extension; AutoOpen>]
module PsiUtil =
    let private getModuleSymbolScope withReferences (psiModule: IPsiModule) =
        psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, withReferences, true)

    [<Extension; CompiledName("GetSymbolScope")>]
    let getSymbolScope (psiModule: IPsiModule) =
        getModuleSymbolScope true psiModule

    [<Extension; CompiledName("GetModuleOnlySymbolScope")>]
    let getModuleOnlySymbolScope (psiModule: IPsiModule) =
        getModuleSymbolScope false psiModule

    [<Extension; CompiledName("GetTokenTypeSafe")>]
    let getTokenType ([<CanBeNull>] node: ITreeNode) =
        if isNotNull node then node.GetTokenType() else null
