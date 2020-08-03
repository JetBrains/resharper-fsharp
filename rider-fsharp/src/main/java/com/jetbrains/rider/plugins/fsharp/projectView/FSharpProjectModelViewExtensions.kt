package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.projectView.ProjectModelViewExtensions
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.nodes.ProjectModelNode

class FSharpProjectModelViewExtensions(project: Project) : ProjectModelViewExtensions(project) {
    override fun getBestParentProjectModelNode(targetLocation: VirtualFile, originalNode: ProjectModelNode?): ProjectModelNode? {
        val nodes = ProjectModelViewHost.getInstance(project).getItemsByVirtualFile(targetLocation).toList()
        if (nodes.isEmpty() || !nodes[0].isFromFSharpProject()) return null
        return nodes.maxBy { it.getSortKey() }
    }
}
