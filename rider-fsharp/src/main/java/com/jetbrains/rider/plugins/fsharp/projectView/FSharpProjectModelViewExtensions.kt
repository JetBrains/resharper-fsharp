package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectView.ProjectModelViewExtensions
import com.jetbrains.rider.projectView.nodes.ProjectModelNode

class FSharpProjectModelViewExtensions(project: Project) : ProjectModelViewExtensions(project) {
    override fun chooseBestProjectModelNode(nodes: List<ProjectModelNode>): ProjectModelNode? {
        if (nodes.isEmpty() || !nodes[0].isFromFSharpProject()) return null
        return nodes.maxBy { it.getSortKey() }
    }
}
