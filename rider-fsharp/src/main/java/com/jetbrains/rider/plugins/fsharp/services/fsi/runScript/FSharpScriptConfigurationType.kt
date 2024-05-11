package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.openapi.project.PossiblyDumbAware
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import com.jetbrains.rider.plugins.fsharp.FSharpIcons

class FSharpScriptConfigurationType : ConfigurationTypeBase(
  ID,
  DisplayName,
  FSharpBundle.message("Fsi.runConfiguration.type.description"),
  FSharpIcons.FSharpConsole
), PossiblyDumbAware {
  private val factory = FSharpRunScriptConfigurationFactory(this)

  //TODO:
  //override fun getHelpTopic(): String = "Run_Debug_Configuration_FSharp_Script"

  init {
    addFactory(factory)
  }

  companion object {
    const val ID = "RunFSharpScript"
    val DisplayName = FSharpBundle.message("Fsi.runConfiguration.type.display.name")
    internal fun getInstance() = ConfigurationTypeUtil.findConfigurationType(FSharpScriptConfigurationType::class.java)
  }
}
