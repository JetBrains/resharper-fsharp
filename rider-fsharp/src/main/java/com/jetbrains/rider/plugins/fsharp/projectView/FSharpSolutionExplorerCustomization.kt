package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.ide.projectView.PresentationData
import com.intellij.openapi.project.Project
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.model.RdProjectFileDescriptor
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerCustomization

class FSharpSolutionExplorerCustomization(project: Project) : SolutionExplorerCustomization(project) {

    override fun updateNode(presentation: PresentationData, node: ProjectModelNode) {
        val descriptor = node.descriptor
        if (descriptor is RdProjectFileDescriptor && FSharpMoveProviderExtension.isSpecialCompileType(descriptor)) {
            presentation.addText(" [${descriptor.buildAction}]", SimpleTextAttributes.GRAY_ATTRIBUTES)
        }
    }
}