package com.jetbrains.rider.editorActions

import com.intellij.openapi.editor.Editor
import com.jetbrains.rdclient.editorActions.FrontendTypedHandler

class FSharpTypedHandler : FrontendTypedHandler() {
    override fun isWritableTypingAssist(editor: Editor, typedChar: Char) = false
    override fun executeTypedActionOnBackend(actionId: String): Boolean = false
}