namespace JetBrains.ReSharper.Plugins.FSharp

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open JetBrains.Application
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features.BackgroundTasks
open JetBrains.Util

type private TrackedOperation =
    {
        StackTrace : StackTrace
        OpName : string
    }

type IFcsReactorMonitor =
    abstract RegisterOperation : opName: string -> string

[<ShellComponent>]
type FcsReactorMonitor
        (
            lifetime: Lifetime,
            locks: IShellLocks,
            backgroundTaskHost: RiderBackgroundTaskHost,
            threading: IThreading,
            configurations: RunsProducts.ProductConfigurations,
            logger: ILogger
        ) as this =
    inherit TraceListener("FcsReactorMonitor")

    /// How long after the reactor becoming busy that the background task should be shown
    let showDelay =
        if configurations.IsInternalMode() then 3.0 else 5.0
        |> TimeSpan.FromSeconds

    let operations = ConcurrentDictionary<Guid, TrackedOperation>()

    /// How long after the reactor becoming free that the background task should be hidden
    let hideDelay = TimeSpan.FromSeconds 0.5

    /// How long an operation must be running for before a stack trace of the enqueuing thread is dumped
    let dumpStackDelay = TimeSpan.FromSeconds 0.1

    let isReactorBusy = new Property<bool>("isReactorBusy")
    let currentTrackedOperation = new Property<TrackedOperation option>("currentTrackedOperation")
    let operationCount = new Property<int64>("operationCount")

    let taskHeader = new Property<string>("taskHeader")
    let taskDescription = new Property<string>("taskDescription")
    let showBackgroundTask = new Property<bool>("showBackgroundTask")

    let opStartRegex = Regex(@"--> (.+) \((.+)\), remaining \d+$", RegexOptions.Compiled)

    let createNewTask (activeLifetime: Lifetime) =
        locks.Dispatcher.AssertAccess()

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
            showDelay,
            fun () -> backgroundTaskHost.AddNewTask(activeLifetime, task)
        )

    let onOperationStart (opName: string) (opArg: string) =
        locks.Dispatcher.AssertAccess()
        operationCount.SetValue(operationCount.Value + 1L) |> ignore

        // Try to parse a GUID from the front of the opName
        let opName =
            if opName.Length > 38 && opName.[0] = '{' then
                match Guid.TryParse(opName.Substring(0, 38)) with
                | false, _ -> opName
                | true, opGuid ->

                match operations.TryRemove opGuid with
                | false, _ -> ()
                | true, trackedOp ->
                    // If we're tracking an operation with this GUID, set the current tracked operation
                    currentTrackedOperation.SetValue(Some trackedOp) |> ignore

                opName.Substring 38
            else
                opName

        taskHeader.SetValue opName |> ignore

        let opArg = if Path.IsPathRooted opArg then Path.GetFileName opArg else opArg
        taskDescription.SetValue(sprintf "%s (operation #%d)" opArg operationCount.Value) |> ignore

        isReactorBusy.SetValue true |> ignore

    let onOperationEnd () =
        locks.Dispatcher.AssertAccess()

        currentTrackedOperation.SetValue None |> ignore
        isReactorBusy.SetValue false |> ignore

    let onTrace (message: string) =
        // todo: add and use a proper reactor event interface in FCS instead of matching trace messages

        if message.Contains "<--" then
            locks.ExecuteOrQueue(lifetime, "FcsReactorMonitor.OnOperationEnd", onOperationEnd)
        else
            let opStartMatch = opStartRegex.Match(message)
            if opStartMatch.Success then
                locks.ExecuteOrQueue(lifetime, "FcsReactorMonitor.OnOperationStart", fun () ->
                    onOperationStart (opStartMatch.Groups.[1].Value) (opStartMatch.Groups.[2].Value))

    do
        showBackgroundTask.WhenTrue(lifetime, Action<_> createNewTask)

        let sequence = SequentialLifetimes lifetime
        currentTrackedOperation.Change.Advise(lifetime, fun (args: PropertyChangedEventArgs<TrackedOperation option>) ->
            sequence.TerminateCurrent()

            match args.New with
            | None -> ()
            | Some trackedOp ->

            threading.QueueAt(sequence.Next(), "FcsReactorMonitor.DumpSlowOperation", dumpStackDelay, fun () ->
                logger.Warn("Operation '{0}' is taking a long time. Stack trace of operation:\n{1}", trackedOp.OpName, trackedOp.StackTrace)))

        isReactorBusy.WhenTrue(lifetime, fun lt -> showBackgroundTask.SetValue true |> ignore)

        isReactorBusy.WhenFalse(lifetime, fun lt ->
            threading.QueueAt(lt, "FcsReactorMonitor.HideTask", hideDelay, fun () ->
                showBackgroundTask.SetValue false |> ignore))

        // Start listening for trace events
        Trace.Listeners.Add(this) |> ignore
        lifetime.OnTermination(fun () -> Trace.Listeners.Remove(this)) |> ignore

    override x.Write(_: string) = ()
    override x.WriteLine(message: string) = if message.StartsWith "Reactor:" then onTrace message

    interface IFcsReactorMonitor with
        override __.RegisterOperation opName =
            // Only take stack traces when trace logging is enabled
            if not (logger.IsEnabled LoggingLevel.TRACE) then opName else

            let stackTrace = StackTrace(1, true)
            let opGuid = Guid.NewGuid()
            operations.TryAdd(opGuid, { StackTrace = stackTrace; OpName = opName }) |> ignore

            (opGuid.ToString "B") + opName
