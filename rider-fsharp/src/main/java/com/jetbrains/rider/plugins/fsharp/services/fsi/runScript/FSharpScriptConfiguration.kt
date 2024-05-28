package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.configuration.EnvironmentVariablesComponent
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.configurations.RuntimeConfigurationError
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.JDOMExternalizerUtil
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptFileType
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import com.jetbrains.rider.run.configurations.DotProfilingAwareRunConfiguration
import com.jetbrains.rider.run.configurations.RiderAsyncRunConfiguration
import org.jdom.Element

//TODO: IRiderDebuggable
class FSharpScriptConfiguration(name: String,
                                var scriptFile: VirtualFile?,
                                var envs: Map<String, String>,
                                project: Project,
                                factory: ConfigurationFactory) :
  RiderAsyncRunConfiguration(name,
                             project,
                             factory,
                             { FSharpScriptConfigurationEditor(it) },
                             FSharpScriptExecutorFactory()), IRiderDebuggable, DotProfilingAwareRunConfiguration {
  companion object {
    private const val SCRIPT_PATH = "SCRIPT_PATH"
  }

  override fun checkConfiguration() {
    super.checkConfiguration()
    if (scriptFile == null || !scriptFile!!.exists() || scriptFile!!.extension != FSharpScriptFileType.defaultExtension)
      throw RuntimeConfigurationError(FSharpBundle.message("Fsi.runConfiguration.has.invalid.script.file.path"))
  }

  override fun readExternal(element: Element) {
    super.readExternal(element)
    scriptFile = JDOMExternalizerUtil.readField(element, SCRIPT_PATH)?.toVirtualFile(true)
    val savedEnvs = mutableMapOf<String, String>()
    EnvironmentVariablesComponent.readExternal(element, savedEnvs)
    envs = savedEnvs
  }

  override fun writeExternal(element: Element) {
    super.writeExternal(element)
    JDOMExternalizerUtil.writeField(element, SCRIPT_PATH, scriptFile?.path)
    EnvironmentVariablesComponent.writeExternal(element, envs)
  }

  override fun clone(): RunConfiguration {
    val newConfiguration = FSharpScriptConfiguration(name, scriptFile, envs, project, factory!!)
    newConfiguration.doCopyOptionsFrom(this)
    copyCopyableDataTo(newConfiguration)
    return newConfiguration
  }

  override fun getConfigurationEditor() = FSharpScriptConfigurationEditor(project)
  override fun supportsProfiling(executorId: String) = false
  override fun suggestedName() = scriptFile?.name
}
