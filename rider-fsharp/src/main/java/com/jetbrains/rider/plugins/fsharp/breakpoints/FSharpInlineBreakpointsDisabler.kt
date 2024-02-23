package com.jetbrains.rider.plugins.fsharp.breakpoints

import com.intellij.lang.LanguageUtil
import com.intellij.openapi.editor.Document
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.xdebugger.breakpoints.InlineBreakpointsDisabler
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

class FSharpInlineBreakpointsDisabler : InlineBreakpointsDisabler {
  override fun areInlineBreakpointsDisabled(document: Document): Boolean {
    return LanguageUtil.getFileLanguage(FileDocumentManager.getInstance().getFile(document)) == FSharpLanguage
  }
}