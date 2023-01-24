package com.jetbrains.rider.plugins.fsharp.settings

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.ServiceManager
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.util.xmlb.XmlSerializerUtil
import com.jetbrains.rider.settings.foldings.RiderCodeFoldingOptionsProvider
import com.jetbrains.rider.settings.foldings.RiderCodeFoldingSettings

class FSharpCodeFoldingProvider : RiderCodeFoldingOptionsProvider<FSharpCodeFoldingSettings>(FSharpCodeFoldingSettings.instance, "F#")

@Suppress("unused")
@State(name = "FSharpCodeFoldingSettings", storages = [(Storage("editor.codeinsight.xml"))])
class FSharpCodeFoldingSettings : RiderCodeFoldingSettings(), PersistentStateComponent<FSharpCodeFoldingSettings> {
  var collapseHashDirectives by foldingCheckBox(
    "ReSharper F# Hash Directives Block Folding",
    "F# hash directives blocks",
    true
  )

  override fun getState(): FSharpCodeFoldingSettings = this
  override fun loadState(state: FSharpCodeFoldingSettings) = XmlSerializerUtil.copyBean(state, this)

  companion object {
    val instance: FSharpCodeFoldingSettings by lazy { ServiceManager.getService(FSharpCodeFoldingSettings::class.java) }
  }
}
