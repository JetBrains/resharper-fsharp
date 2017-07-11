package com.jetbrains.rider.settings

import com.intellij.lang.Language
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

class FSharpCodeStyleSettingsProvider : RiderCodeStyleSettingsProvider() {
    override fun getHelpTopic() = ""

    override fun getLanguage(): Language = FSharpLanguage

    override fun getConfigurableDisplayName(): String {
        return language.displayName
    }

    override fun getPagesId(): Map<String, String> {
        return mapOf("FSharpCodeStylePage" to "Formatting Style")
    }
}
