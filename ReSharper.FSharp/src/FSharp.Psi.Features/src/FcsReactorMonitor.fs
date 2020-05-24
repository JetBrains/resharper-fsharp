module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.FcsReactorMonitor

open System
open System.Diagnostics
open System.Text.RegularExpressions
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features.BackgroundTasks

[<SolutionComponent>]
type FcsReactorMonitor(lifetime: Lifetime, locks: IShellLocks, backgroundTaskHost: RiderBackgroundTaskHost, threading: IThreading) as this =
    inherit TraceListener("FcsReactorMonitor")

    /// How long before the FCS becoming busy that the background task should be shown
    let showDelay = TimeSpan.FromSeconds 1.0

    /// How long after the FCS becoming free that the background task should be hidden
    let hideDelay = TimeSpan.FromSeconds 0.5

    let isReactorBusy = new Property<bool>("isReactorBusy")
    let operationCounter = new Property<int64>("operationCounter")
    let queueLength = new Property<int>("queueLength")

    let taskHeader = new Property<string>("header")
    let taskDescription = new Property<string>("description")
    let taskVisible = new Property<bool>("taskVisible")

    let opStartRegex = Regex(@"--> (.+), remaining (\d+)$", RegexOptions.Compiled)
    let enqueueRegex = Regex(@"enqueue .+, length (\d+)$", RegexOptions.Compiled)

    let createNewTask (activeLifetime: Lifetime) =
        locks.Dispatcher.AssertAccess()

        let task =
            RiderBackgroundTaskBuilder.Create()
                .WithTitle("F# Compiler Service")
                .WithHeader(taskHeader)
                .WithDescription(taskDescription)
                .AsIndeterminate()
                .AsNonCancelable()
                .Build()

        // Show the background task after we've been busy for some time
        threading.QueueAt(
            activeLifetime,
            "FcsReactorMonitor.AddNewTask",
            showDelay,
            fun () -> backgroundTaskHost.AddNewTask(activeLifetime, task)
        )

    let onOperationEnqueue (remaining: int) =
        locks.Dispatcher.AssertAccess()

        // Trace is sent _before_ operation is enqueued, so it's always one behind
        queueLength.SetValue (remaining + 1) |> ignore

    let onOperationStart (opDescription: string) (remaining: int) =
        locks.Dispatcher.AssertAccess()

        operationCounter.SetValue (operationCounter.Value + 1L) |> ignore
        taskHeader.SetValue opDescription |> ignore
        queueLength.SetValue remaining |> ignore
        isReactorBusy.SetValue true |> ignore

    let onOperationEnd () =
        locks.Dispatcher.AssertAccess()
        isReactorBusy.SetValue false |> ignore

    let updateTaskDescription () =
        let queueLength = queueLength.Value

        if queueLength <= 0 then ""
        else sprintf " + %d queued" queueLength
        |> sprintf "#%d%s" operationCounter.Value
        |> taskDescription.SetValue
        |> ignore

    let onTrace (message: string) =
        // todo: this is horrible. add a proper IReactorListener interface in FCS instead

        if message.Contains "<--" then
            locks.Dispatcher.BeginInvoke(lifetime, "FcsReactorMonitor.OnOperationEnd", onOperationEnd)
        else

        let enqueueMatch = enqueueRegex.Match(message)
        if enqueueMatch.Success then
            locks.Dispatcher.BeginInvoke(lifetime, "FcsReactorMonitor.OnOperationEnqueue", fun () -> onOperationEnqueue (Int32.Parse (enqueueMatch.Groups.[1].Value)))

        else
        let opStartMatch = opStartRegex.Match(message)
        if opStartMatch.Success then
            locks.Dispatcher.BeginInvoke(lifetime, "FcsReactorMonitor.OnOperationStart", fun () -> onOperationStart (opStartMatch.Groups.[1].Value) (Int32.Parse (opStartMatch.Groups.[2].Value)))

    do
        // todo: are we running in internal mode?

        taskVisible.WhenTrue(lifetime, Action<_> createNewTask)

        isReactorBusy.WhenTrue(lifetime, fun _ -> taskVisible.SetValue true |> ignore)
        isReactorBusy.WhenFalse(lifetime, fun lt -> threading.QueueAt(lt, "FcsReactorMonitor.HideTask", hideDelay, fun () -> taskVisible.SetValue false |> ignore))

        operationCounter.Change.Advise(lifetime, updateTaskDescription)
        queueLength.Change.Advise(lifetime, updateTaskDescription)

        // Start listening for trace events
        Trace.Listeners.Add(this) |> ignore
        lifetime.OnTermination(fun () -> Trace.Listeners.Remove(this)) |> ignore

    override x.Write(message: string) = ()
    override x.WriteLine(message: string) = if message.StartsWith "Reactor:" then onTrace message
