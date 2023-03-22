package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.lexer.Lexer
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IElementType
import com.intellij.psi.tree.IFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderParserDefinitionBase
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpLexer

class FSharpParserDefinition : RiderParserDefinitionBase(FSharpFileElementType, FSharpFileType) {
  companion object {
    val FSharpElementType = IElementType("RIDER_FSHARP", FSharpLanguage)
    val FSharpFileElementType = RiderFileElementType("RIDER_FSHARP_FILE", FSharpLanguage, FSharpElementType)
  }

  override fun createLexer(project: Project?): Lexer = FSharpLexer()
  override fun getFileNodeType(): IFileElementType = FSharpFileElementType
}
