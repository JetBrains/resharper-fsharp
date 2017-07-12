namespace JetBrains.ReSharper.Plugins.FSharp.Common.Util

[<AutoOpen>]
module CommonUtil =
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Threading
    open JetBrains.Application
    open JetBrains.Application.Progress
    open JetBrains.DocumentModel
    open JetBrains.ProjectModel
    open JetBrains.ProjectModel.Properties
    open JetBrains.ProjectModel.Properties.CSharp
    open JetBrains.ProjectModel.Properties.Managed
    open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
    open JetBrains.Util
    open JetBrains.Util.dataStructures.TypedIntrinsics
    open Microsoft.FSharp.Compiler

    let private interruptCheckTimeout = 30

    let inline isNotNull x = not (isNull x)
    
    let inline isApplicable (project: IProject) =
        match project.ProjectProperties with
        | :? FSharpProjectProperties -> true
        | :? ProjectKCSharpProjectProperties as coreProperties ->
            // todo: remove when ProjectK properties replaced with DotNetCoreProjectFlavour
            coreProperties.ProjectTypeGuids.Contains(FSharpProjectPropertiesFactory.FSharpProjectTypeGuid)
        | _ -> false
        
    let ensureAbsolute (path: FileSystemPath) (projectDirectory: FileSystemPath) =
        let relativePath = path.AsRelative()
        if isNull relativePath then path
        else projectDirectory.Combine(relativePath)
        
    [<CompiledName("RunSynchronouslyWithTimeout")>]
    let runSynchronouslyWithTimeout (action: Func<_>) timeout =
        Async.RunSynchronously(async { return action.Invoke() }, timeout)

    type Async<'T> with
        member x.RunAsTask(?interruptChecker) = // todo: cache these exntension methods in fs cache provider
            let interruptChecker = defaultArg interruptChecker (Action(fun _ -> InterruptableActivityCookie.CheckAndThrow()))
            let cancellationTokenSource = new CancellationTokenSource()
            let cancellationToken = cancellationTokenSource.Token
            let task = Async.StartAsTask(x, cancellationToken = cancellationToken)

            while not task.IsCompleted do
                let finished = task.Wait(interruptCheckTimeout, cancellationToken)
                if not finished then
                    try interruptChecker.Invoke()
                    with :? ProcessCancelledException ->
                        cancellationTokenSource.Cancel()
                        reraise()
            task.Result

    type ILogger with // todo: ILogger not mapped to R# here (ILogger is a decl here?)
        member x.LogFSharpErrors context errors =
            let messages = Seq.fold (fun s (e: FSharpErrorInfo) -> s + "\n" + e.Message) "" errors
            x.LogMessage(LoggingLevel.WARN, sprintf "%s: %s" context messages)

    type IDictionary<'TKey, 'TValue> with
        member x.remove (key: 'TKey) = x.Remove key |> ignore
        member x.add (key: 'TKey, value: 'TValue) = x.Add(key, value) |> ignore
        member x.contains (key: 'TKey) = x.ContainsKey key

    type ISet<'T> with
        member x.remove el = x.Remove el |> ignore
        member x.add el = x.Add el |> ignore

    type FileSystemPath with
        member x.IsImplFile() =
            let ext = x.ExtensionNoDot
            ext = "fs" || ext = "ml"

        member x.IsSigFile() =
            let ext = x.ExtensionNoDot
            ext = "fsi" || ext = "mli"

    type Line = Int32<DocLine>
    type Column = Int32<DocColumn>

    type Range.range with
        member x.GetStartLine()   = x.StartLine - 1 |> Line.op_Explicit
        member x.GetEndLine()     = x.EndLine - 1   |> Line.op_Explicit
        member x.GetStartColumn() = x.StartColumn   |> Column.op_Explicit
        member x.GetEndColumn()   = x.EndColumn     |> Column.op_Explicit
        
        member x.ToDocumentRange(document: IDocument) =
            let startOffset = document.GetLineStartOffset(x.GetStartLine()) + x.StartColumn
            let endOffset = document.GetLineStartOffset(x.GetEndLine()) + x.EndColumn
            DocumentRange(document, TextRange(startOffset, endOffset))