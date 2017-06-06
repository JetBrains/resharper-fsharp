package com.jetbrains.resharper.settings

import com.intellij.lang.Language
import com.jetbrains.resharper.ideaInterop.fileTypes.fsharp.FSharpLanguage

class FSharpCodeStyleSettingsProvider : RiderCodeStyleSettingsProvider() {
    override fun getLanguage(): Language = FSharpLanguage

    override fun getConfigurableDisplayName(): String {
        return language.displayName
    }

    override fun getPagesId(): Map<String, String> {
        return mapOf("FSharpIndentStylePage" to "Formatting Style")
    }
}

