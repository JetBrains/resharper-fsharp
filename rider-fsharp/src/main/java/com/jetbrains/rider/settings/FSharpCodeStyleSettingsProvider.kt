package com.jetbrains.rider.settings

import com.intellij.lang.Language
import com.intellij.psi.codeStyle.CodeStyleSettings
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptLanguage

class FSharpCodeStyleSettingsProvider : FSharpCodeStyleSettingsProviderBase(FSharpLanguage)

abstract class FSharpCodeStyleSettingsProviderBase(private val lang: Language) : RiderLanguageCodeStyleSettingsProvider() {
    override fun createConfigurable(baseSettings: CodeStyleSettings, modelSettings: CodeStyleSettings) =
            createRiderConfigurable(baseSettings, modelSettings, language, configurableDisplayName)

    override fun getLanguage() = lang
    override fun getHelpTopic() = "Settings_Code_Style_FSHARP"
    override fun getConfigurableDisplayName() = lang.displayName
    override fun getPagesId() = mapOf("FSharpCodeStylePage" to "Formatting Style", "FantomasPage" to "Fantomas")
}
