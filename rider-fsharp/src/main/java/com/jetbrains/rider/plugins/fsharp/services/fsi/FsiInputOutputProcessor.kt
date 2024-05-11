package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.console.LanguageConsoleImpl
import com.intellij.execution.impl.ConsoleViewUtil
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.editor.markup.HighlighterLayer
import com.intellij.openapi.editor.markup.HighlighterTargetArea
import com.intellij.util.concurrency.ThreadingAssertions
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.highlighting.FSharpSyntaxHighlighter
import com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners.FsiConsoleRunnerBase

class FsiInputOutputProcessor(private val fsiRunner: FsiConsoleRunnerBase) {
  private var isInitialText = true
  private var nextOutputTextIsFirst = true

  private val fSharpSyntaxHighlighter = FSharpSyntaxHighlighter()

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
    ThreadingAssertions.assertEventDispatchThread()
    printText(text + "\n", FsiIcons.COMMAND_MARKER, fSharpSyntaxHighlighter, outputType)
    EditorUtil.scrollToTheEnd(fsiRunner.consoleView.historyViewer)

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
          printText(text, fsiResultIconWithTooltip, fSharpSyntaxHighlighter, outputType)

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
