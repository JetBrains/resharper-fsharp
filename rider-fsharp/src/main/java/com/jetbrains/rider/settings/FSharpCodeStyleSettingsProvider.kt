package com.jetbrains.rider.settings

import com.intellij.lang.Language
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptLanguage

class FSharpCodeStyleSettingsProvider : FSharpCodeStyleSettingsProviderBase(FSharpLanguage)
class FSharpScriptCodeStyleSettingsProvider : FSharpCodeStyleSettingsProviderBase(FSharpScriptLanguage)

abstract class FSharpCodeStyleSettingsProviderBase(private val lang: Language) : RiderCodeStyleSettingsProvider() {
    override fun getLanguage() = lang
    override fun getHelpTopic() = "Settings_Code_Style_FSHARP"
    override fun getConfigurableDisplayName() = lang.displayName
    override fun getPagesId() = mapOf("FSharpCodeStylePage" to "Formatting Style")
}
