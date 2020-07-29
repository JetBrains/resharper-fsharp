package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.console.LanguageConsoleImpl
import com.intellij.execution.impl.ConsoleViewImpl
import com.intellij.execution.impl.ConsoleViewUtil
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.openapi.command.WriteCommandAction
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.editor.markup.HighlighterLayer
import com.intellij.openapi.editor.markup.HighlighterTargetArea
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.highlighting.FSharpSyntaxHighlighter

class FsiInputOutputProcessor(private val fsiRunner: FsiConsoleRunner) {
    private var isInitialText = true
    private var nextOutputTextIsFirst = true

    private val fSharpSyntaxHighlighter = FSharpSyntaxHighlighter()

    private fun textOffsets(text: String): Pair<Int, Int> {
        (fsiRunner.consoleView as ConsoleViewImpl).flushDeferredText()

        val historyEditor = fsiRunner.consoleView.historyViewer
        val startOffset = historyEditor.document.textLength
        val endOffset = startOffset + text.length

        return Pair(startOffset, endOffset)
    }

    private fun fsiIconWithTooltipOnOutputText(outputType: ConsoleViewContentType): IconWithTooltip? {
        if (nextOutputTextIsFirst) {
            when (outputType) {
                ConsoleViewContentType.NORMAL_OUTPUT -> return FsiIcons.RESULT
                ConsoleViewContentType.ERROR_OUTPUT -> return FsiIcons.ERROR
            }
        }

        return null
    }

    fun printInputText(text: String, outputType: ConsoleViewContentType) {
        printText(text + "\n", FsiIcons.COMMAND_MARKER, fSharpSyntaxHighlighter, outputType)
        EditorUtil.scrollToTheEnd(fsiRunner.consoleView.historyViewer)

        nextOutputTextIsFirst = true
    }

    fun printOutputText(text: String, outputType: ConsoleViewContentType) {
        if (isInitialText) {
            printOutputInitialText(text, outputType)
        } else {
            val fsiResultIconWithTooltip = fsiIconWithTooltipOnOutputText(outputType)

            when (outputType) {
                ConsoleViewContentType.NORMAL_OUTPUT ->
                    printText(text, fsiResultIconWithTooltip, fSharpSyntaxHighlighter, outputType)
                ConsoleViewContentType.ERROR_OUTPUT ->
                    printText(text, fsiResultIconWithTooltip, null, outputType)
            }

            nextOutputTextIsFirst = false
        }
    }

    private fun printText(text: String, iconWithTooltip: IconWithTooltip?, highlighter: FSharpSyntaxHighlighter?,
                          outputType: ConsoleViewContentType) =
            WriteCommandAction.runWriteCommandAction(fsiRunner.project) {
                val (startOffset, endOffset) = textOffsets(text)

                if (highlighter == null) {
                    fsiRunner.consoleView.print(text, outputType)
                } else {
                    ConsoleViewUtil.printWithHighlighting(fsiRunner.consoleView, text, highlighter)
                }

                (fsiRunner.consoleView as LanguageConsoleImpl).flushDeferredText()

                if (iconWithTooltip == null) return@runWriteCommandAction

                fsiRunner.consoleView.historyViewer.markupModel.addRangeHighlighter(
                        startOffset, endOffset, HighlighterLayer.LAST, null, HighlighterTargetArea.LINES_IN_RANGE
                ).apply { gutterIconRenderer = FsiConsoleIndicatorRenderer(iconWithTooltip) }
            }

    private fun printOutputInitialText(text: String, outputType: ConsoleViewContentType) =
            WriteCommandAction.runWriteCommandAction(fsiRunner.project) {
                fsiRunner.consoleView.print(text, outputType)
            }

    fun onServerPrompt() {
        isInitialText = false
    }
}
