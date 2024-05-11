package com.jetbrains.rider.plugins.fsharp.services.fsi.runScript

import com.intellij.execution.lineMarker.ExecutorAction
import com.intellij.execution.lineMarker.RunLineMarkerContributor

import com.intellij.icons.AllIcons
import com.intellij.psi.PsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpScriptFileImpl

class FSharpRunScriptMarkerContributor : RunLineMarkerContributor() {
  override fun getInfo(element: PsiElement): Info? {
    if (element !is FSharpScriptFileImpl) return null
    return Info(AllIcons.Actions.Execute, ExecutorAction.getActions()) {
      FSharpScriptConfigurationType.DisplayName
    }
  }
}
