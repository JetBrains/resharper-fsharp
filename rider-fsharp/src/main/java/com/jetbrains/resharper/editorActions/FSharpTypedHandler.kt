package com.jetbrains.resharper.editorActions

import com.intellij.openapi.editor.Editor

class FSharpTypedHandler : RiderTypedHandler() {
    override fun isWritableTypingAssist(editor: Editor, typedChar: Char): Boolean = false
}