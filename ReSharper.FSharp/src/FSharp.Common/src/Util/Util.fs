namespace JetBrains.ReSharper.Plugins.FSharp.Common.Util

[<AutoOpen>]
module rec CommonUtil =
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Threading
    open JetBrains.Application
    open JetBrains.Application.Progress
    open JetBrains.DataFlow
    open JetBrains.DocumentModel
    open JetBrains.ProjectModel
    open JetBrains.ProjectModel.ProjectsHost
    open JetBrains.ProjectModel.Properties
    open JetBrains.ProjectModel.Properties.CSharp
    open JetBrains.ProjectModel.Properties.Managed
    open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
    open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
    open JetBrains.ReSharper.Psi.Modules
    open JetBrains.Util
    open JetBrains.Util.dataStructures.TypedIntrinsics
    open Microsoft.FSharp.Compiler
    open Microsoft.FSharp.Compiler.SourceCodeServices

    /// Reference equality.
    let inline (==) a b = LanguagePrimitives.PhysicalEquality a b

    /// Reference inequality.
    let inline (!=) a b = not (a == b)

    let private interruptCheckTimeout = 30

    let inline isNotNull x = not (isNull x)
    
    let inline (|NotNull|_|) x =
        if isNull x then None else Some()

    [<Literal>]
    let FsprojExtension = "fsproj"

    let isFSharpProject (guids: Guid seq) (projectFile: FileSystemPath) =
        equalsIgnoreCase FsprojExtension projectFile.ExtensionNoDot ||
        Seq.exists FSharpProjectPropertiesFactory.IsKnownProjectTypeGuid guids

    let (|FSharpProjectMark|_|) (mark: IProjectMark) =
        if isFSharpProject [mark.Guid] mark.Location then Some() else None

    let ensureAbsolute (path: FileSystemPath) (projectDirectory: FileSystemPath) =
        match path.AsRelative() with
        | null -> path
        | relativePath -> projectDirectory.Combine(relativePath)

    let concatErrors errors =
        Seq.fold (fun s (e: FSharpErrorInfo) -> s + "\n" + e.Message) "" errors

    [<CompiledName("DecompileOpName")>]
    let decompileOpName name =
        PrettyNaming.DecompileOpName name
        
    [<CompiledName("RunSynchronouslyWithTimeout")>]
    let runSynchronouslyWithTimeout (action: Func<_>) timeout =
        Async.RunSynchronously(async { return action.Invoke() }, timeout)

    type Async<'T> with
        member x.RunAsTask(?interruptChecker) =
            let interruptChecker = defaultArg interruptChecker (Action(fun _ -> InterruptableActivityCookie.CheckAndThrow()))
            let cancellationTokenSource = new CancellationTokenSource()
            let cancellationToken = cancellationTokenSource.Token
            let task = Async.StartAsTask(x, cancellationToken = cancellationToken)

            while not task.IsCompleted do
                let finished = task.Wait(interruptCheckTimeout, cancellationToken)
                if not finished then
                    try interruptChecker.Invoke()
                    with :? OperationCanceledException ->
                        cancellationTokenSource.Cancel()
                        reraise()
            task.Result

    type IDictionary<'TKey, 'TValue> with
        member x.remove (key: 'TKey) = x.Remove key |> ignore
        member x.add (key: 'TKey, value: 'TValue) = x.Add(key, value) |> ignore
        member x.contains (key: 'TKey) = x.ContainsKey key

    type ISet<'T> with
        member x.remove el = x.Remove el |> ignore
        member x.add el = x.Add el |> ignore

    let fsExtensions = ["fs"; "fsi"; "ml"; "mli"; "fsx"; "fsscript"] |> Set.ofList
    let dllExtensions = ["dll"; "exe"] |> Set.ofList

    let (|ImplFile|_|) (path: FileSystemPath) =
        match path.ExtensionNoDot with
        | "fs" | "ml" -> Some()
        | _ -> None

    let (|SigFile|_|) (path: FileSystemPath) =
        match path.ExtensionNoDot with
        | "fsi" | "mli" -> Some()
        | _ -> None

    type Line = Int32<DocLine>
    type Column = Int32<DocColumn>
    type FileSystemPath = JetBrains.Util.FileSystemPath

    let inline docLine (x: int)   = Line.op_Explicit(x)
    let inline docColumn (x: int) = Column.op_Explicit(x)

    type Range.range with
        member inline x.GetStartLine()   = x.StartLine - 1 |> docLine
        member inline x.GetEndLine()     = x.EndLine - 1   |> docLine
        member inline x.GetStartColumn() = x.StartColumn   |> docColumn
        member inline x.GetEndColumn()   = x.EndColumn     |> docColumn

        member x.ToTextRange(document: IDocument) =
            let startOffset = document.GetLineStartOffset(x.GetStartLine()) + x.StartColumn
            let endOffset = document.GetLineStartOffset(x.GetEndLine()) + x.EndColumn
            TextRange(startOffset, endOffset)
        
        member x.ToDocumentRange(document: IDocument) =
            DocumentRange(document, x.ToTextRange(document))

    type IProject with
        member x.IsFSharp =
            isFSharpProject x.ProjectProperties.ProjectTypeGuids x.ProjectFileLocation

    let (|FSharpProject|_|) (projectModelElement: IProjectModelElement) =
        match projectModelElement with
        | :? IProject as project when project.IsFSharp -> Some project
        | _ -> None

    let tryGetValue (key: 'TKey) (dictionary: IDictionary<'TKey,'TValue>) =
        let res = ref Unchecked.defaultof<'TValue>
        match dictionary.TryGetValue(key, res), res with
        | true, value -> Some !value
        | _ -> None

    let (|AddRemoveArgs|) (args: AddRemoveEventArgs<_>) =
        args.Value

    let (|KeyValuePair|) (pair: KeyValuePair<_,_>) =
        pair.Key, pair.Value

    let (|Pair|) (pair: Pair<_,_>) =
        pair.First, pair.Second

    let (|PsiModuleReference|) (ref: IPsiModuleReference) =
        ref.Module

    let (|ProjectFile|ProjectFolder|UnknownProjectItem|) (projectItem: IProjectItem) =
        match projectItem with
        | :? IProjectFile as file -> ProjectFile file
        | :? IProjectFolder as folder -> ProjectFolder folder
        | _ -> UnknownProjectItem

    let equalsIgnoreCase other (string: string) =
        string.Equals(other, StringComparison.OrdinalIgnoreCase)

    let eq a b = a = b

    let getCommonParent path1 path2 =
        FileSystemPath.GetDeepestCommonParent(path1, path2)

    let (|UnixSeparators|) (path: FileSystemPath) =
        path.NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix)

    let setComparer =
        { new IEqualityComparer<HashSet<_>> with
            member this.Equals(x, y) = x.SetEquals(y)
            member this.GetHashCode(x) = x.Count }

[<AutoOpen>]
module rec FSharpMsBuildUtils =
    open BuildActions
    open ItemTypes
    open JetBrains.Platform.MsBuildHost.Models
    open JetBrains.ProjectModel

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
        if isCompileBefore itemType then Some () else None

    let (|CompileAfter|_|) itemType =
        if isCompileAfter itemType then Some () else None

    let (|Folder|_|) (itemType: string) =
        if equalsIgnoreCase folderItemType itemType then Some () else None

    let (|BuildAction|) itemType =
        BuildAction.GetOrCreate(itemType)

    let (|RdItem|) (item: RdProjectItem) =
        RdItem (item.ItemType, item.EvaluatedInclude)

    let (|SourceFile|_|) (buildAction: BuildAction) =
        if buildAction.IsCompile() || buildAction = compileBefore || buildAction = compileAfter then Some () else None

    let (|Resource|_|) buildAction =
        if buildAction = BuildAction.RESOURCE then Some () else None

    let changesOrder = function
        | CompileBefore | CompileAfter -> true
        | _ -> false

    type BuildAction with
        member x.ChangesOrder() = changesOrder x.Value
