package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.openapi.util.IconLoader
import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase

object FSharpFileType : RiderLanguageFileTypeBase(FSharpLanguage) {
    override fun getDefaultExtension() = "fs"
    override fun getDescription() = "F# file"
    override fun getIcon() = IconLoader.getIcon("/icons/fsharp.png")
    override fun getName() = "F#"
}
