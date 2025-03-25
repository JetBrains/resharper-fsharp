package com.jetbrains.rider.plugins.fsharp.editorActions

import com.intellij.codeInsight.editorActions.BackspaceHandler
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.editor.Caret
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.actionSystem.EditorActionHandler

class FSharpBackspaceHandlerDelegate(val originalHandler: EditorActionHandler) : BackspaceHandler(originalHandler) {
  override fun executeWriteAction(editor: Editor, caret: Caret, dataContext: DataContext?) {
    val document = editor.document.charsSequence[caret.offset]
    myOriginalHandler.execute(editor, caret, dataContext)
  }
}
