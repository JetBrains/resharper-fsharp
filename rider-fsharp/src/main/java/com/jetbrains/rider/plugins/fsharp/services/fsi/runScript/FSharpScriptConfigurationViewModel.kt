package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.run.configurations.controls.ControlBase
import com.jetbrains.rider.run.configurations.controls.EnvironmentVariablesEditor
import com.jetbrains.rider.run.configurations.controls.PathSelector
import com.jetbrains.rider.run.configurations.controls.RunConfigurationViewModelBase

open class FSharpScriptConfigurationViewModel(
  val lifetime: Lifetime,
  val project: Project,
  val scriptFileSelector: PathSelector,
  val environmentVariablesEditor: EnvironmentVariablesEditor,
) : RunConfigurationViewModelBase() {

  override val controls: List<ControlBase>
    get() = listOf(
      scriptFileSelector,
      environmentVariablesEditor,
    )

  fun reset(
    scriptPath: String,
    envs: Map<String, String>
  ) {
    scriptFileSelector.path.set(scriptPath)
    environmentVariablesEditor.envs.set(envs)
  }
}
