package com.jetbrains.rider.plugins.fsharp.services.settings

import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class FsiOptionsPage : SimpleOptionsPage("F# Interactive", "FsiOptionsPage") {
  override fun getId(): String = "Fsi"
}