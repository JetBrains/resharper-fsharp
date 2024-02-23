package com.jetbrains.rider.plugins.fsharp.breakpoints

import com.intellij.lang.LanguageUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.xdebugger.breakpoints.InlineBreakpointsDisabler
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

class FSharpInlineBreakpointsDisabler : InlineBreakpointsDisabler {
  override fun areInlineBreakpointsDisabled(virtualFile: VirtualFile?): Boolean {
    return LanguageUtil.getFileLanguage(virtualFile) == FSharpLanguage
  }
}