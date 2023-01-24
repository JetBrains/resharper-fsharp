package com.jetbrains.rider.plugins.fsharp.actions

import com.intellij.ide.scratch.ScratchFileCreationHelper
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.project.Project
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptLanguage

class FSharpScratchCreationHelper : ScratchFileCreationHelper() {
  override fun prepareText(project: Project, context: Context, dataContext: DataContext): Boolean {
    context.language = FSharpScriptLanguage
    context.fileExtension = "fsx"
    return false
  }
}
