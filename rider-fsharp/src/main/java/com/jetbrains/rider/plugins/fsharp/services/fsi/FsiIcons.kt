package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.icons.AllIcons
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import javax.swing.Icon

data class IconWithTooltip(val icon: Icon, val tooltip: String?)

object FsiIcons {
  val COMMAND_MARKER = IconWithTooltip(AllIcons.Actions.Execute, FSharpBundle.message("FSI.icons.command.marker.tooltip.text"))
  val RESULT = IconWithTooltip(AllIcons.Vcs.Equal, FSharpBundle.message("FSI.icons.result.tooltip.text"))
  val ERROR = IconWithTooltip(AllIcons.General.Warning, FSharpBundle.message("FSI.icons.error.tooltip.text"))
}
