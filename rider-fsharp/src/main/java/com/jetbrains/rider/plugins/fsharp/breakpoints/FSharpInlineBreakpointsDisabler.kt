package com.jetbrains.rider.plugins.fsharp.breakpoints

import com.intellij.lang.LanguageUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.xdebugger.breakpoints.InlineBreakpointsDisabler
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

// we have to disable inline breakpoints in F# due to the usability issues described in RIDER-106329
class FSharpInlineBreakpointsDisabler : InlineBreakpointsDisabler {
  override fun areInlineBreakpointsDisabled(virtualFile: VirtualFile?): Boolean {
    return LanguageUtil.getFileLanguage(virtualFile) == FSharpLanguage
  }
}