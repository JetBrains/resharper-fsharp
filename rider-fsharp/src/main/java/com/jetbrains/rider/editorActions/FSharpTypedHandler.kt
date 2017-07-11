package com.jetbrains.rider.editorActions

import com.intellij.openapi.editor.Editor

class FSharpTypedHandler : RiderTypedHandler() {
    override fun isWritableTypingAssist(editor: Editor, typedChar: Char) = false
    override val syncBackspace = false
    override val syncEnter = false
    override val syncTab = false
    override val syncDelete = false
}