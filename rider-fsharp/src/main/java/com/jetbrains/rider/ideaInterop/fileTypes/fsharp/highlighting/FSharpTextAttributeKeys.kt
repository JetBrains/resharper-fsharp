package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.highlighting

import com.jetbrains.rider.colors.IRiderTextAttributeKeys
import com.jetbrains.rider.colors.RiderLanguageTextAttributeKeys

object FSharpTextAttributeKeys : IRiderTextAttributeKeys {
  val KEYWORD = FSharpTextAttributeKeys.key("ReSharper.FSHARP_KEYWORD", RiderLanguageTextAttributeKeys.KEYWORD)
  val PREPROCESSOR_KEYWORD = FSharpTextAttributeKeys.key(
    "ReSharper.FSHARP_PREPROCESSOR_KEYWORD",
    RiderLanguageTextAttributeKeys.PREPROCESSOR_KEYWORD
  )

  val STRING = FSharpTextAttributeKeys.key("ReSharper.FSHARP_STRING", RiderLanguageTextAttributeKeys.STRING)
  val NUMBER = FSharpTextAttributeKeys.key("ReSharper.FSHARP_NUMBER", RiderLanguageTextAttributeKeys.NUMBER)
  val COMMENT =
    FSharpTextAttributeKeys.key("ReSharper.FSHARP_LINE_COMMENT", RiderLanguageTextAttributeKeys.LINE_COMMENT)
  val BLOCK_COMMENT =
    FSharpTextAttributeKeys.key("ReSharper.FSHARP_BLOCK_COMMENT", RiderLanguageTextAttributeKeys.BLOCK_COMMENT)
}
