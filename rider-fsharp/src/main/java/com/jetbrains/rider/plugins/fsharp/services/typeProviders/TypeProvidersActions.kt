package com.jetbrains.rider.plugins.fsharp.services.typeProviders

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution

class RestartTypeProvidersAction : AnAction("Restart Type Providers") {
    override fun update(e: AnActionEvent) {
        val project = e.project
        if (project != null) {
            e.presentation.isEnabledAndVisible = project.solution.rdFSharpModel.fSharpTypeProvidersHost.isLaunched.sync(Unit)
        }
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project
        if (project != null) {
            project.solution.rdFSharpModel.fSharpTypeProvidersHost.restartTypeProviders.start(project.lifetime, Unit)
        }
    }
}
