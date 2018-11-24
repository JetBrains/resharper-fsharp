package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.ide.projectView.PresentationData
import com.intellij.openapi.project.Project
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.nodes.getUserData
import com.jetbrains.rider.projectView.solutionExplorer.SolutionExplorerCustomization

class FSharpSolutionExplorerCustomization(project: Project) : SolutionExplorerCustomization(project) {
    override fun updateNode(presentation: PresentationData, node: ProjectModelNode) {
        val compileType = node.descriptor.getUserData(FSharpMoveProviderExtension.FSharpCompileType) ?: return
        presentation.addText(" ($compileType)", SimpleTextAttributes.GRAY_ITALIC_ATTRIBUTES)
    }
}