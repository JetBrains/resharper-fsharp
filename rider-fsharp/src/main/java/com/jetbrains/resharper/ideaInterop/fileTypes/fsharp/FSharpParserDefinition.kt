package com.jetbrains.resharper.ideaInterop.fileTypes.fsharp

import com.intellij.lexer.DummyLexer
import com.intellij.lexer.Lexer
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IElementType
import com.jetbrains.resharper.ideaInterop.fileTypes.RiderFileElementType
import com.jetbrains.resharper.ideaInterop.fileTypes.RiderParserDefinitionBase

class FSharpParserDefinition : RiderParserDefinitionBase(FSharpFileElementType, FSharpFileType) {
    companion object {
        val FSharpElementType = IElementType("RIDER_FSHARP", FSharpLanguage)
        val FSharpFileElementType = RiderFileElementType("RIDER_FSHARP_FILE", FSharpLanguage, FSharpElementType)
    }

    override fun createLexer(project: Project?): Lexer = DummyLexer(FSharpElementType)

}