package com.jetbrains.rider.plugins.fsharp

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.registry.Registry
import com.intellij.openapi.util.registry.RegistryValue
import com.intellij.openapi.util.registry.RegistryValueListener
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.model.rdFSharpModel
import com.jetbrains.rider.projectView.solution

class FSharpHost(project: Project) : LifetimedProjectComponent(project) {
    private val fSharpModel = project.solution.rdFSharpModel

    companion object {
        const val registryKey = "rider.fsharp.experimental"
    }

    init {
        val registryValue = Registry.get(registryKey)
        fSharpModel.enableExperimentalFeatures.set(registryValue.asBoolean())
        registryValue.addListener(object : RegistryValueListener.Adapter() {
            override fun afterValueChanged(value: RegistryValue) {
                fSharpModel.enableExperimentalFeatures.set(value.asBoolean())
            }
        }, project)
    }
}
