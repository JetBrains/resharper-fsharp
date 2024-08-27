package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.actions.ConfigurationContext
import com.intellij.execution.actions.LazyRunConfigurationProducer
import com.intellij.openapi.actionSystem.ActionPlaces
import com.intellij.openapi.util.Ref
import com.intellij.psi.PsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpScriptFileImpl

class FSharpRunScriptConfigurationProducer : LazyRunConfigurationProducer<FSharpScriptConfiguration>() {
  private fun getFileFromContext(context: ConfigurationContext) =
    context.location?.psiElement?.containingFile as? FSharpScriptFileImpl

  override fun isConfigurationFromContext(configuration: FSharpScriptConfiguration, context: ConfigurationContext): Boolean {
    val file = getFileFromContext(context) ?: return false
    return configuration.scriptFile == file.virtualFile
  }

  override fun setupConfigurationFromContext(
    configuration: FSharpScriptConfiguration,
    context: ConfigurationContext,
    sourceElement: Ref<PsiElement>
  ): Boolean {
    if (context.place == ActionPlaces.INTENTION_MENU) return false
    val scriptFile = getFileFromContext(context) ?: return false
    configuration.scriptFile = scriptFile.virtualFile
    configuration.setGeneratedName()
    return true
  }

  override fun getConfigurationFactory() =
    FSharpRunScriptConfigurationFactory(FSharpScriptConfigurationType.getInstance())
}
