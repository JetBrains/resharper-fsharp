package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessHandler
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.ui.ConsoleView
import com.jetbrains.rider.plugins.fsharp.services.fsi.FsiHost
import com.jetbrains.rider.plugins.fsharp.services.fsi.FsiProcessHandler
import com.jetbrains.rider.run.DebugProfileStateBase
import com.jetbrains.rider.run.DotNetProfileConsoleViewProviderExtension
import com.jetbrains.rider.run.IDotNetProfileState

class FSharpScriptConsoleViewProvider : DotNetProfileConsoleViewProviderExtension {
  override fun isApplicable(executionEnvironment: ExecutionEnvironment) =
    executionEnvironment.runProfile is FSharpScriptConfiguration

  override suspend fun getProcessHandler(
    executionEnvironment: ExecutionEnvironment, state: IDotNetProfileState, commandLine: GeneralCommandLine
  ): ProcessHandler {
    val project = executionEnvironment.project
    val fsiHost = FsiHost.getInstance(project)
    val presentableCommandLineString =
      if (state is DebugProfileStateBase) state.createPresentableCommandLine() else null

    val fsiRunner = fsiHost.createConsoleRunner(
      "", executionEnvironment.project, executionEnvironment.executor, commandLine, presentableCommandLineString
    )

    return fsiRunner.processHandler
  }

  override suspend fun getConsoleView(
    executionEnvironment: ExecutionEnvironment, processHandler: ProcessHandler
  ): ConsoleView = (processHandler as FsiProcessHandler).consoleRunner.consoleView
}
