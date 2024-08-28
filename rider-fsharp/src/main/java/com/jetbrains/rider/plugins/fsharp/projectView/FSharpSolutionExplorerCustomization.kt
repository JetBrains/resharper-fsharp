package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.ide.projectView.PresentationData
import com.intellij.openapi.project.Project
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerCustomization
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity

class FSharpSolutionExplorerCustomization(project: Project) : SolutionExplorerCustomization(project) {

  override fun updateNode(presentation: PresentationData, entity: ProjectModelEntity) {
    val descriptor = entity.descriptor
    val specialCompileType = getSpecialCompileType(descriptor)
    if (specialCompileType != null) {
      presentation.addText(" [${specialCompileType}]", SimpleTextAttributes.GRAY_ATTRIBUTES)
    }
  }

  override fun ignoreFoldersOnTop(entity: ProjectModelEntity) = entity.isFromFSharpProject()
}
