package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.console.LanguageConsoleImpl
import com.intellij.execution.impl.ConsoleViewUtil
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.openapi.command.WriteCommandAction
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.LineNumberConverter
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.editor.markup.HighlighterLayer
import com.intellij.openapi.editor.markup.HighlighterTargetArea
import com.intellij.util.concurrency.ThreadingAssertions
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.highlighting.FSharpSyntaxHighlighter
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.highlighting.FsiOutputSyntaxHighlighter
import com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners.FsiConsoleRunnerBase

internal class FsiInputOutputProcessor(val fsiRunner: FsiConsoleRunnerBase) {
  private var isInitialText = true
  private var nextOutputTextIsFirst = true

  private val fSharpSyntaxHighlighter = FSharpSyntaxHighlighter.Instance
  private val fsiOutputSyntaxHighlighter = FsiOutputSyntaxHighlighter.Instance

  private fun fsiIconWithTooltipOnOutputText(outputType: ConsoleViewContentType): IconWithTooltip? {
    if (nextOutputTextIsFirst) {
      when (outputType) {
        ConsoleViewContentType.NORMAL_OUTPUT -> return FsiIcons.RESULT
        ConsoleViewContentType.ERROR_OUTPUT -> return FsiIcons.ERROR
      }
    }

    return null
  }

  fun printInputText(text: String, outputType: ConsoleViewContentType, lineStart: Int) {
    ThreadingAssertions.assertEventDispatchThread()

    WriteCommandAction.runWriteCommandAction(fsiRunner.project) {
      (fsiRunner.consoleView as LanguageConsoleImpl).flushDeferredText()
      val historyViewer = fsiRunner.consoleView.historyViewer
      val lastLine = maxOf(historyViewer.document.lineCount - 1, 0)
      val linesToAddCount = 1 + text.count { it == '\n' }

      historyViewer.settings.isLineNumbersShown = true
      historyViewer.gutter.setLineNumberConverter(object : LineNumberConverter.Increasing {
        override fun convert(p0: Editor, index: Int) = lineStart + (index - lastLine) - 1
        override fun getMaxLineNumber(editor: Editor) = linesToAddCount
        override fun convertLineNumberToString(editor: Editor, lineNumber: Int) =
          if (lineNumber <= lastLine || lineNumber > lastLine + linesToAddCount) null
          else super.convertLineNumberToString(editor, lineNumber)
      })

      printText(text + "\n", FsiIcons.COMMAND_MARKER, fSharpSyntaxHighlighter, outputType)
      EditorUtil.scrollToTheEnd(historyViewer)
    }

    nextOutputTextIsFirst = true
  }

  fun printOutputText(text: String, outputType: ConsoleViewContentType) {
    ThreadingAssertions.assertEventDispatchThread()
    if (isInitialText) {
      fsiRunner.consoleView.print(text, outputType)
    } else {
      val fsiResultIconWithTooltip = fsiIconWithTooltipOnOutputText(outputType)

      when (outputType) {
        ConsoleViewContentType.NORMAL_OUTPUT ->
          printText(text, fsiResultIconWithTooltip, fsiOutputSyntaxHighlighter, outputType)

        ConsoleViewContentType.ERROR_OUTPUT ->
          printText(text, fsiResultIconWithTooltip, null, outputType)
      }

      nextOutputTextIsFirst = false
    }
  }

  private fun printText(
    text: String, iconWithTooltip: IconWithTooltip?, highlighter: FSharpSyntaxHighlighter?,
    outputType: ConsoleViewContentType
  ) {
    if (highlighter == null) {
      fsiRunner.consoleView.print(text, outputType)
    } else {
      ConsoleViewUtil.printWithHighlighting(fsiRunner.consoleView, text, highlighter)
    }

    if (iconWithTooltip == null) return

    (fsiRunner.consoleView as LanguageConsoleImpl).flushDeferredText()
    val endOffset = fsiRunner.consoleView.historyViewer.document.textLength
    val startOffset = endOffset - text.length
    fsiRunner.consoleView.historyViewer.markupModel.addRangeHighlighter(
      startOffset, endOffset, HighlighterLayer.LAST, null, HighlighterTargetArea.LINES_IN_RANGE
    ).apply { gutterIconRenderer = FsiConsoleIndicatorRenderer(iconWithTooltip) }
  }

  fun onServerPrompt() {
    isInitialText = false
  }
}
