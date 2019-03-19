package com.jetbrains.rider.settings

import com.intellij.openapi.options.Configurable
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class RiderFSharpFileTemplatesOptionPage : SimpleOptionsPage("F#", "RiderFSharpFileTemplatesSettings"), Configurable.NoScroll {
    override fun getId(): String = "RiderFSharpFileTemplatesSettings"
}
