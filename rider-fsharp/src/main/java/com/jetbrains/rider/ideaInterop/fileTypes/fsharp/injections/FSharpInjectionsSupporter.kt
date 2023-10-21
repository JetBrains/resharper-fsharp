package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.injections

import com.intellij.psi.PsiLanguageInjectionHost
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression
import com.jetbrains.rider.languages.fileTypes.clr.psi.patterns.ClrLanguagePatterns
import com.jetbrains.rider.plugins.appender.database.common.ClrLanguageInjectionSupport

object FSharpPatterns : ClrLanguagePatterns(FSharpTokenType.PLUS){
    @JvmStatic
    @Suppress("unused")
    fun fsharpSqlInStringPattern() = super.sqlInStringPattern()
}

class FSharpInjectionSupport : ClrLanguageInjectionSupport() {
  override fun getPatternClasses() = arrayOf(FSharpPatterns.javaClass)
  override fun getId() = FSHARP_SUPPORT_ID
  override fun isApplicableTo(host: PsiLanguageInjectionHost?) = host is FSharpStringLiteralExpression

  companion object {
    const val FSHARP_SUPPORT_ID = "F#"
  }
}
