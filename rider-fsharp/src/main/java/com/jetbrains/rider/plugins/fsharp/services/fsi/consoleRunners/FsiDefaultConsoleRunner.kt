package com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.Separator
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import com.jetbrains.rider.plugins.fsharp.services.fsi.CommandHistoryAction
import com.jetbrains.rider.plugins.fsharp.services.fsi.FsiHost

internal class FsiDefaultConsoleRunner(commandLine: GeneralCommandLine, fsiHost: FsiHost) :
  FsiConsoleRunnerBase(fsiHost.project, fsiTitle, commandLine) {

  companion object {
    val fsiTitle = FSharpBundle.message("Fsi.ConsoleRunner.title")
  }

  override fun getToolBarActions() =
    mutableListOf(
      ResetFsiAction(this.fsiHost),
      Separator(),
      CommandHistoryAction(this),
      Separator(),
      OpenSettings(project))

  private class ResetFsiAction(private val host: FsiHost) :
    AnAction(FSharpBundle.message("Fsi.actions.reset.fsi.title"), null, AllIcons.Actions.Restart) {
    override fun actionPerformed(e: AnActionEvent) {
      host.resetFsiDefaultConsole()
    }
  }
}
