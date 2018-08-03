package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.openapi.fileTypes.LanguageFileType
import com.jetbrains.rider.plugins.fsharp.FSharpIcons

object FSharpFileType : LanguageFileType(FSharpLanguage) {
    override fun getName() = FSharpLanguage.displayName
    override fun getDefaultExtension() = "fs"
    override fun getDescription() = "F# file"
    override fun getIcon() = FSharpIcons.FSharp
}

object FSharpScriptFileType : LanguageFileType(FSharpScriptLanguage) {
    override fun getName() = FSharpScriptLanguage.displayName
    override fun getDefaultExtension() = "fsx"
    override fun getDescription() = "F# script file"
    override fun getIcon() = FSharpIcons.FSharpScript
}