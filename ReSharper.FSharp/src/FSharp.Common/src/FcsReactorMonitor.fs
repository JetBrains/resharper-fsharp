namespace JetBrains.ReSharper.Plugins.FSharp

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open System.Threading
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.Platform.RdFramework.Util
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Host.Features.BackgroundTasks
open JetBrains.Util

[<RequireQualifiedAccess>]
module MonitoredReactorOperation =
    let empty opName =
        { new IMonitoredReactorOperation with
            override __.Dispose () = ()
            override __.OperationName = opName
        }


[<SolutionComponent>]
type FcsReactorMonitor
        (lifetime: Lifetime, backgroundTaskHost: RiderBackgroundTaskHost, threading: IThreading,
         checkerService: FSharpCheckerService, logger: ILogger, solution: ISolution) as this =

    inherit TraceListener("FcsReactorMonitor")

    /// How long after the reactor becoming busy that the background task should be shown
    let showDelay = new Property<TimeSpan>("showDelay")

    let mutable lastOperationId = 0L
    let operations = ConcurrentDictionary<int64, StackTrace>()

    /// How long after the reactor becoming free that the background task should be hidden
    let hideDelay = TimeSpan.FromSeconds 0.5

    /// How long an operation must be running for before a stack trace of the enqueuing thread is dumped
    let dumpStackDelay = TimeSpan.FromSeconds 10.0

    let mutable slowOperationTimer = null
    let isReactorBusy = new Property<bool>("isReactorBusy")

    let taskHeader = new Property<string>("taskHeader")
    let taskDescription = new Property<string>("taskDescription")
    let showBackgroundTask = new Property<bool>("showBackgroundTask")

    let opStartRegex = Regex(@"--> (.+) \((.+)\), remaining \d+$", RegexOptions.Compiled)

    let createNewTask (activeLifetime: Lifetime) =
        let task =
            RiderBackgroundTaskBuilder.Create()
                .WithTitle("F# Compiler Service is busy...")
                .WithHeader(taskHeader)
                .WithDescription(taskDescription)
                .AsIndeterminate()
                .AsNonCancelable()
                .Build()

        // Only show the background task after we've been busy for some time
        threading.QueueAt(
            activeLifetime,
            "FcsReactorMonitor.AddNewTask",
            showDelay.Value,
            fun () -> backgroundTaskHost.AddNewTask(activeLifetime, task))

    /// Called when a FCS operation starts.
    /// Always called from the current FCS reactor thread.
    let onOperationStart (opName: string) (opArg: string) =
        // Try to parse the operation ID from the front of the opName
        let operationId, opName =
            let endIndex = opName.IndexOf '}'
            if endIndex = -1 || opName.[0] <> '{' then None, opName else

            match Int64.TryParse(opName.Substring(1, endIndex - 1)) with
            | false, _ -> None, opName
            | true, operationId ->

            let opName = opName.Substring(endIndex + 1)

            match operations.TryGetValue operationId with
            | null -> None, opName
            | stackTrace ->

            // Warn if the operation still hasn't finished in a few seconds
            slowOperationTimer <-
                let callback _ = logger.Warn("Operation '{0}' ({1}) is taking a long time. Stack trace:\n{2}", opName, opArg, stackTrace)
                new Timer(callback, null, dumpStackDelay, Timeout.InfiniteTimeSpan)

            Some operationId, opName

        taskHeader.SetValue opName |> ignore

        let opArg = if Path.IsPathRooted opArg then Path.GetFileName opArg else opArg
        match operationId with
        | None -> taskDescription.SetValue(opArg) |> ignore
        | Some operationId -> taskDescription.SetValue(sprintf "%s (operation #%d)" opArg operationId) |> ignore

        isReactorBusy.SetValue true |> ignore

    /// Called when a FCS operation ends.
    /// Always called from the current FCS reactor thread.
    let onOperationEnd () =
        // Cancel slow operation timer - the operation is now finished
        if isNotNull slowOperationTimer then
            slowOperationTimer.Dispose()
            slowOperationTimer <- null

        isReactorBusy.SetValue false |> ignore

    let onTrace (message: string) =
        // todo: add and use a proper reactor event interface in FCS instead of matching trace messages

        if message.Contains "<--" then
            onOperationEnd ()
        else
            let opStartMatch = opStartRegex.Match(message)
            if opStartMatch.Success then
                onOperationStart (opStartMatch.Groups.[1].Value) (opStartMatch.Groups.[2].Value)

    do
        solution.RdFSharpModel().FcsBusyDelayMs.FlowInto(lifetime, showDelay,
            fun ms -> TimeSpan.FromMilliseconds(float ms))

        showBackgroundTask.WhenTrue(lifetime, Action<_> createNewTask)

        isReactorBusy.WhenTrue(lifetime, fun lt -> showBackgroundTask.SetValue true |> ignore)

        isReactorBusy.WhenFalse(lifetime, fun lt ->
            threading.QueueAt(lt, "FcsReactorMonitor.HideTask", hideDelay, fun () ->
                showBackgroundTask.SetValue false |> ignore))

        // Start listening for trace events
        checkerService.FcsReactorMonitor <- this
        Trace.Listeners.Add(this) |> ignore
        lifetime.OnTermination(fun () ->
            checkerService.FcsReactorMonitor <- Unchecked.defaultof<_>
            Trace.Listeners.Remove(this)) |> ignore

    override x.Write(_: string) = ()
    override x.WriteLine(message: string) = if message.StartsWith "Reactor:" then onTrace message

    interface IFcsReactorMonitor with
        override __.MonitorOperation opName =
            // Only monitor operations when trace logging is enabled
            if not (logger.IsEnabled LoggingLevel.TRACE) then
                MonitoredReactorOperation.empty opName
            else

            let stackTrace = StackTrace(1, true)
            let operationId = Interlocked.Increment &lastOperationId
            operations.TryAdd(operationId, stackTrace) |> ignore

            { new IMonitoredReactorOperation with
                override __.Dispose () = match operations.TryRemove operationId with _ -> ()
                override __.OperationName = sprintf "{%d}%s" operationId opName
            }
