package com.jetbrains.rider.plugins.fsharp.settings

import com.intellij.openapi.options.ex.SortedConfigurableGroup
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

private class FSharpInlayHintsConfigurableGroup : SortedConfigurableGroup("inlay.hints.RiderInlayHintsFSharpConfigurableGroup",
  FSharpBundle.message("configurable.group.inlay.hints.FSharpInlayHintsConfigurableGroup.settings.display.name"),
  "", null, 10) {

  class FSharpTypeHintsOptionsPage : SimpleOptionsPage("FSharpTypeHintsOptionsPage", "FSharpTypeHintsOptionsPage") {
    override fun getId() = pageId
    override fun getDisplayName() = FSharpBundle.message("configurable.name.type.hints")
  }

  override fun buildConfigurables() = arrayOf(
    FSharpTypeHintsOptionsPage()
  )
}
