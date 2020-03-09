package com.jetbrains.rider.plugins.fsharp

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.registry.Registry
import com.intellij.openapi.util.registry.RegistryValue
import com.intellij.openapi.util.registry.RegistryValueListener
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.model.rdFSharpModel
import com.jetbrains.rider.projectView.solution

class FSharpHost(project: Project) : LifetimedProjectComponent(project) {
    private val fSharpModel = project.solution.rdFSharpModel

    companion object {
        const val experimentalFeaturesRegistryKey = "rider.fsharp.experimental"
        const val formatterRegistryKey = "rider.fsharp.formatter"
    }

    init {
        initRegistryValue(experimentalFeaturesRegistryKey, fSharpModel.enableExperimentalFeatures)
        initRegistryValue(formatterRegistryKey, fSharpModel.enableFormatter)
    }

    private fun initRegistryValue(registryKey: String, property: IOptProperty<Boolean>) {
        val registryValue = Registry.get(registryKey)
        property.set(registryValue.asBoolean())
        registryValue.addListener(object : RegistryValueListener.Adapter() {
            override fun afterValueChanged(value: RegistryValue) {
                property.set(value.asBoolean())
            }
        }, project)
    }
}
