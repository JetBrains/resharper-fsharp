module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.FcsReactorMonitor

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features.BackgroundTasks

//[<SolutionComponent>]
type FcsReactorMonitor
        (
            lifetime: Lifetime,
            locks: IShellLocks,
            backgroundTaskHost: RiderBackgroundTaskHost,
            threading: IThreading,
            configurations: RunsProducts.ProductConfigurations
        ) as this =
    inherit TraceListener("FcsReactorMonitor")

    /// How long after the reactor becoming busy that the background task should be shown
    let showDelay =
        if configurations.IsInternalMode() then 3.0 else 5.0
        |> TimeSpan.FromSeconds

    /// How long after the reactor becoming free that the background task should be hidden
    let hideDelay = TimeSpan.FromSeconds 0.5

    let isReactorBusy = new Property<bool>("isReactorBusy")
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

    let onOperationStart (opDescription: string) (opArg: string) =
        locks.Dispatcher.AssertAccess()

        operationCount.SetValue(operationCount.Value + 1L) |> ignore
        taskHeader.SetValue opDescription |> ignore

        let opArg = if Path.IsPathRooted opArg then Path.GetFileName opArg else opArg
        taskDescription.SetValue(sprintf "%s (operation #%d)" opArg operationCount.Value) |> ignore

        isReactorBusy.SetValue true |> ignore

    let onOperationEnd () =
        locks.Dispatcher.AssertAccess()

        isReactorBusy.SetValue false |> ignore

    let onTrace (message: string) =
        // todo: add and use a proper reactor event interface in FCS instead of matching trace messages

        if message.Contains "<--" then
            locks.Dispatcher.BeginInvoke(lifetime, "FcsReactorMonitor.OnOperationEnd", onOperationEnd)
        else

        let opStartMatch = opStartRegex.Match(message)
        if opStartMatch.Success then
            locks.Dispatcher.BeginInvoke(lifetime, "FcsReactorMonitor.OnOperationStart", fun () ->
                onOperationStart (opStartMatch.Groups.[1].Value) (opStartMatch.Groups.[2].Value))

    do
        showBackgroundTask.WhenTrue(lifetime, Action<_> createNewTask)

        isReactorBusy.WhenTrue(lifetime, fun _ -> showBackgroundTask.SetValue true |> ignore)

        isReactorBusy.WhenFalse(lifetime, fun lt ->
            threading.QueueAt(lt, "FcsReactorMonitor.HideTask", hideDelay, fun () ->
                showBackgroundTask.SetValue false |> ignore))

        // Start listening for trace events
        Trace.Listeners.Add(this) |> ignore
        lifetime.OnTermination(fun () -> Trace.Listeners.Remove(this)) |> ignore

    override x.Write(message: string) = ()
    override x.WriteLine(message: string) = if message.StartsWith "Reactor:" then onTrace message
