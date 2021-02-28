package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rider.projectView.ProjectModelViewExtensions
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.getProjectModelEntities

class FSharpProjectModelViewExtensions(project: Project) : ProjectModelViewExtensions(project) {
    override fun getBestParentProjectModelNode(targetLocation: VirtualFile): ProjectModelEntity? {
        val entities = WorkspaceModel.getInstance(project).getProjectModelEntities(targetLocation, project).toList()
        if (entities.isEmpty() || !entities[0].isFromFSharpProject()) return null
        return entities.maxByOrNull { it.getSortKey() }
    }
}
