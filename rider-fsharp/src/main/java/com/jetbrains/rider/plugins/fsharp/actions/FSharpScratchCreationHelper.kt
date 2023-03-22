package com.jetbrains.rider.plugins.fsharp.actions

import com.intellij.ide.scratch.ScratchFileCreationHelper
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.project.Project

class FSharpScratchCreationHelper : ScratchFileCreationHelper() {
  override fun prepareText(project: Project, context: Context, dataContext: DataContext): Boolean {
    context.fileExtension = "fsx"
    return false
  }
}
