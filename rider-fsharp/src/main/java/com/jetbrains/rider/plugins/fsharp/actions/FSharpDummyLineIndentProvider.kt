package com.jetbrains.rider.plugins.fsharp.actions

import com.jetbrains.rdclient.editorActions.FrontendDummyLineIndentProvider
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

object FSharpDummyLineIndentProvider : FrontendDummyLineIndentProvider(FSharpLanguage)
