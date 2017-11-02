package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.codeInsight.lookup.LookupManager
import com.intellij.openapi.command.WriteCommandAction
import com.intellij.openapi.editor.ex.EditorEx
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.project.Project
import java.awt.event.KeyAdapter
import java.awt.event.KeyEvent

class HistoryKeyListener(
        private val project: Project, private val consoleEditor: EditorEx, private val history: CommandHistory)
    : KeyAdapter(), HistoryUpdateListener {

    private var historyPos = 0
    private var unfinishedCommand = ""

    override fun onNewEntry(entry: CommandHistory.Entry) {
        // reset history positions
        historyPos = history.size
        unfinishedCommand = ""
    }

    private enum class HistoryMove {
        UP, DOWN
    }

    override fun keyReleased(e: KeyEvent) {
        when (e.keyCode) {
            KeyEvent.VK_UP -> moveHistoryCursor(HistoryMove.UP)
            KeyEvent.VK_DOWN -> moveHistoryCursor(HistoryMove.DOWN)
        }
    }

    private fun moveHistoryCursor(move: HistoryMove) {
        if (history.size == 0) return
        if (LookupManager.getInstance(project).activeLookup != null) return

        val caret = consoleEditor.caretModel
        val document = consoleEditor.document

        val curOffset = caret.offset
        val curLine = document.getLineNumber(curOffset)
        val totalLines = document.lineCount

        when (move) {
            HistoryMove.UP -> {
                if (curLine != 0) return

                if (historyPos == history.size) {
                    unfinishedCommand = document.text
                }

                historyPos = Math.max(historyPos - 1, 0)
                WriteCommandAction.runWriteCommandAction(project) {
                    document.setText(history[historyPos].visibleText)
                    EditorUtil.scrollToTheEnd(consoleEditor)
                    caret.moveToOffset(document.textLength)
                }
            }
            HistoryMove.DOWN -> {
                if (historyPos == history.size || curLine != totalLines - 1) return

                historyPos = Math.min(historyPos + 1, history.size)
                WriteCommandAction.runWriteCommandAction(project) {
                    document.setText(if (historyPos == history.size) unfinishedCommand else history[historyPos].visibleText)
                    caret.moveToOffset(document.textLength)
                    EditorUtil.scrollToTheEnd(consoleEditor)
                }
            }
        }
    }
}