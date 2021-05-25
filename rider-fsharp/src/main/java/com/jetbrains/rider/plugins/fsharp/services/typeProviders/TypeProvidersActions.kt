package com.jetbrains.rider.plugins.fsharp.services.typeProviders

import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.actions.ProjectViewActionBase
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.isSolution

class RestartTypeProvidersAction : ProjectViewActionBase("Restart Type Providers", "Restart Type Providers") {
    override fun actionPerformedInternal(entity: ProjectModelEntity, project: Project) {
        project.solution.rdFSharpModel.fSharpTypeProvidersHost.restartTypeProviders.start(project.lifetime, Unit)
    }

    override fun getItemInternal(entity: ProjectModelEntity, project: Project) =
            if (entity.isSolution() && project.solution.rdFSharpModel.fSharpTypeProvidersHost.isLaunched.sync(Unit)) entity else null
}
