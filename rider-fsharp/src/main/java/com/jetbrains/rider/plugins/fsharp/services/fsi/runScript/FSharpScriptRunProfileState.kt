package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.DefaultExecutionResult
import com.intellij.execution.ExecutionException
import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.jetbrains.rider.debugger.showElevationDialogIfNeeded
import com.jetbrains.rider.plugins.fsharp.services.fsi.FsiHost
import com.jetbrains.rider.run.createRunCommandLine
import com.jetbrains.rider.runtime.DotNetExecutable
import com.jetbrains.rider.runtime.DotNetRuntime
import com.jetbrains.rider.util.idea.getComponent

class FSharpScriptRunProfileState(private val dotNetExecutable: DotNetExecutable,
                                  private val dotNetRuntime: DotNetRuntime,
                                  private val environment: ExecutionEnvironment) : RunProfileState {
  override fun execute(executor: Executor, runner: ProgramRunner<*>): ExecutionResult {
    try {
      dotNetExecutable.validate()
      val commandLine = dotNetExecutable.createRunCommandLine(dotNetRuntime)

      val project = environment.project
      val profile = environment.runProfile as FSharpScriptConfiguration
      val fsiHost = project.getComponent<FsiHost>()
      val fsiRunner = fsiHost.createConsoleRunner(profile.name, profile.scriptFile!!.path, project, environment.executor, commandLine)
      dotNetExecutable.onBeforeProcessStarted(environment, environment.runProfile, fsiRunner.processHandler)

      return DefaultExecutionResult(fsiRunner.consoleView, fsiRunner.processHandler, *fsiRunner.getToolBarActions().toTypedArray())
    }
    catch (t: Throwable) {
      showElevationDialogIfNeeded(t, environment.project)
      throw ExecutionException(t)
    }
  }
}
