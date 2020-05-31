package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.icons.AllIcons
import javax.swing.Icon

data class IconWithTooltip(val icon: Icon, val tooltip: String?)

object FsiIcons {
    val COMMAND_MARKER = IconWithTooltip(AllIcons.Actions.Execute, "Executed command")
    val RESULT = IconWithTooltip(AllIcons.Vcs.Equal, "Result")
    val ERROR = IconWithTooltip(AllIcons.General.Warning, "Error")
}
