package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.actions.ProjectViewActionBase
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.projectView.workspace.isProject
import kotlinx.coroutines.launch

class SendProjectReferencesToFsiAction : ProjectViewActionBase() {
  override fun actionPerformedInternal(entity: ProjectModelEntity, project: Project) {
    val id = entity.getId(project) ?: return
    val fSharpInteractiveHost = project.solution.rdFSharpModel.fSharpInteractiveHost
    val fsiHost = FsiHost.getInstance(project)

    fsiHost.coroutineScope.launch {
      val references = fSharpInteractiveHost.getProjectReferences.startSuspending(id)
      val text = references.joinToString("\n") { "#r @\"$it\"" } +
        "\n" +
        "# 1 \"stdin\"\n;;\n"
      fsiHost.sendToFsi(text, text)
    }
  }

  override fun getItemInternal(entity: ProjectModelEntity, project: Project) = if (entity.isProject()) entity else null
}
