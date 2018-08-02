package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.openapi.fileTypes.LanguageFileType
import com.intellij.openapi.util.IconLoader

object FSharpFileType : LanguageFileType(FSharpLanguage) {
    override fun getName() = FSharpLanguage.displayName
    override fun getDefaultExtension() = "fs"
    override fun getDescription() = "F# file"
    override fun getIcon() = IconLoader.getIcon("/icons/Fsharp.png")
}

object FSharpScriptFileType : LanguageFileType(FSharpScriptLanguage) {
    override fun getName() = FSharpScriptLanguage.displayName
    override fun getDefaultExtension() = "fsx"
    override fun getDescription() = "F# script file"
    override fun getIcon() = IconLoader.getIcon("/icons/FsharpScript.png")
}