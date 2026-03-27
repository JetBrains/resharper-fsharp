package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.ExecutionResult
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.ui.ConsoleView
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.model.debuggerWorker.DebuggerWorkerModel
import com.jetbrains.rider.plugins.fsharp.services.fsi.FsiHost
import com.jetbrains.rider.plugins.fsharp.services.fsi.FsiProcessHandler
import com.jetbrains.rider.run.DebugProfileStateBase
import com.jetbrains.rider.run.IDotNetDebugProfileState

class FSharpScriptDebugProfileState(
    private val environment: ExecutionEnvironment,
    private val wrappedState: IDotNetDebugProfileState
) : IDotNetDebugProfileState by wrappedState {

    override suspend fun startDebuggerWorker(
        workerCmd: GeneralCommandLine,
        protocolModel: DebuggerWorkerModel,
        protocolServerPort: Int,
        projectLifetime: Lifetime
    ): DebuggerWorkerProcessHandler {
        val project = environment.project
        val fsiHost = FsiHost.getInstance(project)
        val presentableCommandLineString =
            if (wrappedState is DebugProfileStateBase) wrappedState.createPresentableCommandLine() else null

        val fsiRunner = fsiHost.createConsoleRunner(
            "", environment.project, environment.executor, workerCmd, presentableCommandLineString
        )

        return DebuggerWorkerProcessHandler(fsiRunner.processHandler, protocolModel, attached, workerCmd.commandLineString, projectLifetime)
    }

    override suspend fun execute(
        workerConsole: ConsoleView,
        workerProcessHandler: DebuggerWorkerProcessHandler,
        lifetime: Lifetime
    ): ExecutionResult {
        val console = (workerProcessHandler.debuggerWorkerRealHandler as FsiProcessHandler).consoleRunner.consoleView
        return super.execute(console, workerProcessHandler, lifetime)
    }
}
