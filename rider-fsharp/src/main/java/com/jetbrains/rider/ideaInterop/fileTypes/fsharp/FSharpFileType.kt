package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.plugins.fsharp.FSharpIcons

object FSharpFileType : RiderLanguageFileTypeBase(FSharpLanguage) {
  override fun getName() = "F#"
  override fun getDisplayName() = "F#"
  override fun getDefaultExtension() = "fs"
  override fun getDescription() = "F# file"
  override fun getIcon() = FSharpIcons.FSharp
}

object FSharpScriptFileType : RiderLanguageFileTypeBase(FSharpLanguage) {
  override fun getName() = "F# Script"
  override fun getDisplayName() = "F# Script"
  override fun getDefaultExtension() = "fsx"
  override fun getDescription() = "F# script file"
  override fun getIcon() = FSharpIcons.FSharpScript
}