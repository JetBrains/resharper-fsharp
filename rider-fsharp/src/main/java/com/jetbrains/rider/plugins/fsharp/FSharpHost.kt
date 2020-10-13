package com.jetbrains.rider.plugins.fsharp

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.registry.Registry
import com.intellij.openapi.util.registry.RegistryValue
import com.intellij.openapi.util.registry.RegistryValueListener
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution

class FSharpHost(project: Project) : LifetimedProjectComponent(project) {
    private val fSharpModel = project.solution.rdFSharpModel

    companion object {
        const val fcsBusyDelayRegistryKey = "rider.fsharp.fcsBusyDelay.ms"
    }

    init {
        initRegistryValue(fcsBusyDelayRegistryKey, RegistryValue::asInteger, fSharpModel.fcsBusyDelayMs)
    }

    private fun <T : Any> initRegistryValue(registryKey: String, registryToValue: (registryValue: RegistryValue) -> T, property: IOptProperty<T>) {
        val registryValue = Registry.get(registryKey)
        property.set(registryToValue(registryValue))
        registryValue.addListener(object : RegistryValueListener.Adapter() {
            override fun afterValueChanged(value: RegistryValue) {
                property.set(registryToValue(value))
            }
        }, project)
    }
}
