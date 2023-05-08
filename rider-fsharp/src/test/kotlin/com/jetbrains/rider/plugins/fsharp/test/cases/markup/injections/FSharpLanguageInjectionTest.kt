package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.intellij.codeInsight.daemon.impl.HighlightInfoType
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithMarkup
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.getHighlighters
import com.jetbrains.rider.test.waitForDaemon
import org.testng.annotations.Test

@TestEnvironment(solution = "CoreConsoleApp", sdkVersion = SdkVersion.DOT_NET_6)
class FSharpLanguageInjectionTest : BaseTestWithMarkup() {
  private fun doTest() {
    doTestWithMarkupModel("Program.fs", "Program.fs") {
      waitForDaemon()
      printStream.print(
        getHighlighters(
          project!!, this
        ) {
          it.severity === HighlightInfoType.INJECTED_FRAGMENT_SEVERITY
        }
      )
    }
  }

  @Test
  fun testInjectionByCommentInRegularStrings() = doTest()

  @Test
  fun testInjectionByCommentInVerbatimStrings() = doTest()

  @Test
  fun testInjectionByCommentInRegularInterpolatedStrings() = doTest()

  @Test
  fun testInjectionByCommentInVerbatimInterpolatedStrings() = doTest()

  @Test
  fun testInjectionByCommentInTripleQuotedStrings() = doTest()

  @Test
  fun testInjectionByCommentInTripleQuotedInterpolatedStrings() = doTest()

  @Test
  fun testInjectionByAlternativeComment() = doTest()

  @Test
  fun testInjectionByCommentWithPrefixAndSuffix() = doTest()

  @Test
  fun testEscapeSequences() = doTest()

  @Test
  fun testInjectionByAnnotationInRegularStrings() = doTest()

  @Test
  fun testInjectionByAnnotationInVerbatimStrings() = doTest()

  @Test
  fun testInjectionByAnnotationInRegularInterpolatedStrings() = doTest()

  @Test
  fun testInjectionByAnnotationInVerbatimInterpolatedStrings() = doTest()

  @Test
  fun testInjectionByAnnotationInTripleQuotedStrings() = doTest()

  @Test
  fun testInjectionByAnnotationInTripleQuotedInterpolatedStrings() = doTest()

  @Test
  fun testInjectionByAnnotationWithPrefixAndSuffix() = doTest()

  @Test
  fun testInjectionByFunction() = doTest()
}
