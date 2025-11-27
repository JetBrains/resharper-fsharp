package com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners

import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.ui.RunContentDescriptor
import com.intellij.openapi.actionSystem.Separator
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.plugins.fsharp.services.fsi.CommandHistoryAction

internal class FsiScriptProfileConsoleRunner(
  @NlsSafe consoleName: String,
  project: Project,
  private val executor: Executor,
  commandLine: GeneralCommandLine,
  presentableCommandLineString: String? = null
) :
  FsiConsoleRunnerBase(project, consoleName, commandLine, presentableCommandLineString) {
  override fun getExecutor() = executor
  override fun getToolBarActions() =
    mutableListOf(
      Separator(),
      CommandHistoryAction(this),
      Separator(),
      OpenSettings(project))

  override fun showConsole(defaultExecutor: Executor?, contentDescriptor: RunContentDescriptor) {}
}
