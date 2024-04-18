package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.openapi.fileChooser.FileChooserDescriptor
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.guessProjectDir
import com.intellij.openapi.util.Comparing
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.util.io.FileUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptFileType
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import com.jetbrains.rider.run.RiderRunBundle
import com.jetbrains.rider.run.configurations.ProtocolLifetimedSettingsEditor
import com.jetbrains.rider.run.configurations.controls.ControlViewBuilder
import com.jetbrains.rider.run.configurations.controls.EnvironmentVariablesEditor
import com.jetbrains.rider.run.configurations.controls.PathSelector
import javax.swing.JComponent

class FSharpScriptConfigurationEditor(private val project: Project) : ProtocolLifetimedSettingsEditor<FSharpScriptConfiguration>() {
  private lateinit var viewModel: FSharpScriptConfigurationViewModel

  override fun createEditor(lifetime: Lifetime): JComponent {
    val pathSelector =
      PathSelector(
        FSharpBundle.message("Fsi.runConfiguration.label.script.path.with.colon"),
        "Script_path",
        FileChooserDescriptor(
          /* chooseFiles = */ true,
          /* chooseFolders = */ false,
          /* chooseJars = */ false,
          /* chooseJarsAsFiles = */ false,
          /* chooseJarContents = */ false,
          /* chooseMultiple = */ false
        ).withFileFilter { file ->
          Comparing.equal(file.extension, FSharpScriptFileType.defaultExtension, SystemInfo.isFileSystemCaseSensitive)
        }, lifetime)
    pathSelector.rootDirectory.set(project.guessProjectDir()?.path)

    viewModel = FSharpScriptConfigurationViewModel(
      lifetime,
      project,
      pathSelector,
      EnvironmentVariablesEditor(RiderRunBundle.message("label.environment.variables.with.colon"), "Environment_variables"))

    return ControlViewBuilder(lifetime, project, FSharpScriptConfigurationType.ID).build(viewModel)
  }

  override fun applyEditorTo(configuration: FSharpScriptConfiguration) {
    configuration.scriptFile = FileUtil.toSystemIndependentName(viewModel.scriptFileSelector.path.value).toVirtualFile(true)
    configuration.envs = viewModel.environmentVariablesEditor.envs.value
  }

  override fun resetEditorFrom(configuration: FSharpScriptConfiguration) {
    configuration.apply {
      viewModel.reset(
        scriptFile?.path ?: "",
        envs,
      )
    }
  }
}
