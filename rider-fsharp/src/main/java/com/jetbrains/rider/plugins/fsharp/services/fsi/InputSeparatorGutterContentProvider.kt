package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.console.BasicGutterContentProvider
import com.intellij.openapi.editor.Document
import com.intellij.openapi.editor.Editor

class InputSeparatorGutterContentProvider(isLineRelationshipComputable: Boolean)
    : BasicGutterContentProvider(isLineRelationshipComputable) {

    private val separatorLines = mutableSetOf<Int>()

    override fun doIsShowSeparatorLine(line: Int, editor: Editor, document: Document): Boolean {
        val actualEditorLine = line + 2
        return separatorLines.contains(actualEditorLine)
    }

    fun addLineSeparator(line: Int) {
        separatorLines.add(line)
    }

    override fun beforeEvaluate(editor: Editor) = Unit
}
