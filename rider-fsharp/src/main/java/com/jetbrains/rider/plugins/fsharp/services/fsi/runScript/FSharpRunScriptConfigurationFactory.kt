package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.configurations.RunConfigurationSingletonPolicy
import com.intellij.openapi.project.Project
import com.jetbrains.rider.run.configurations.DotNetConfigurationFactoryBase
import org.jetbrains.annotations.NotNull

class FSharpRunScriptConfigurationFactory(type: FSharpScriptConfigurationType) :
  DotNetConfigurationFactoryBase<FSharpScriptConfiguration>(type) {
  override fun getId() = "F# Script"
  override fun getSingletonPolicy() = RunConfigurationSingletonPolicy.SINGLE_INSTANCE_ONLY
  override fun createTemplateConfiguration(@NotNull project: Project) =
    FSharpScriptConfiguration("F# Script", null, mapOf(), project, this)
}
