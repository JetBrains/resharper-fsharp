package com.jetbrains.rider.plugins.fsharp.services.settings

import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class FsiOptionsPage : SimpleOptionsPage(FSharpBundle.message("Options.fsi.page.title"), "FsiOptionsPage") {
  override fun getId(): String = "Fsi"
}