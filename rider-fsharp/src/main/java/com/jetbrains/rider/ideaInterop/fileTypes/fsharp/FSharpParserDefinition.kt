package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.lexer.DummyLexer
import com.intellij.lexer.Lexer
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderParserDefinitionBase
import com.intellij.psi.tree.IFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpLexer

class FSharpParserDefinition : RiderParserDefinitionBase(FSharpFileElementType, FSharpFileType) {
  companion object {
    val FSharpElementType = IElementType("RIDER_FSHARP", FSharpLanguage)
    val FSharpFileElementType = RiderFileElementType("RIDER_FSHARP_FILE", FSharpLanguage, FSharpElementType)
  }

  override fun createLexer(project: Project?): Lexer = FSharpLexer()
  override fun getFileNodeType(): IFileElementType = FSharpFileElementType
}

class FSharpScriptParserDefinition : RiderParserDefinitionBase(FSharpScriptFileElementType, FSharpScriptFileType) {
  companion object {
    val FSharpScriptElementType = IElementType("RIDER_FSHARP_SCRIPT", FSharpScriptLanguage)
    val FSharpScriptFileElementType =
      RiderFileElementType("RIDER_FSHARP_SCRIPT_FILE", FSharpScriptLanguage, FSharpScriptElementType)
  }

  override fun createLexer(project: Project?): Lexer = FSharpLexer()
  override fun getFileNodeType(): IFileElementType = FSharpParserDefinition.FSharpFileElementType

}
