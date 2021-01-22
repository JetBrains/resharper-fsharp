package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.getComponent
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.actions.ProjectViewActionBase
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.projectView.workspace.isProject

class SendProjectReferencesToFsiAction : ProjectViewActionBase("Send project references", "Send project references") {
    override fun actionPerformedInternal(entity: ProjectModelEntity, project: Project) {
        val id = entity.getId(project) ?: return
        val fSharpInteractiveHost = project.solution.rdFSharpModel.fSharpInteractiveHost
        fSharpInteractiveHost.getProjectReferences.start(project.lifetime, id).result.adviseOnce(Lifetime.Eternal) { result ->
            val text = result.unwrap().joinToString("\n") { "#r @\"$it\"" } +
                    "\n" +
                    "# 1 \"stdin\"\n;;\n"
            project.getComponent<FsiHost>().sendToFsi(text, text, false)
        }
    }

    override fun getItemInternal(entity: ProjectModelEntity, project: Project) = if (entity.isProject()) entity else null
}
