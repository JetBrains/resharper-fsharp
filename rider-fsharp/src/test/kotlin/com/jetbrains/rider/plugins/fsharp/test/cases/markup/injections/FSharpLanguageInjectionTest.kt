package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.intellij.codeInsight.daemon.impl.HighlightInfoType
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithMarkup
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.getHighlighters
import com.jetbrains.rider.test.waitForDaemon
import org.testng.annotations.Test

private fun BaseTestWithMarkup.doTest() {
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


@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
@Solution("CoreConsoleApp")
class FSharpLanguageInjectionTest : BaseTestWithMarkup() {
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

  //TODO: fix lexer for second case
  @Test
  fun testInjectionByCommentInRawStrings() = doTest()

  @Test
  fun testInjectionByCommentInTripleQuotedInterpolatedStrings() = doTest()

  @Test
  fun testEscapeSequences() = doTest()

  @Test
  fun testInjectionByAnnotation() = doTest()

  @Test
  fun testInjectionByFunction() = doTest()
}

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
@Solution("FableApp")
class FSharpFrontendFrameworksTest : BaseTestWithMarkup() {
  @Test
  fun testInjectionByExternalAnnotation() = doTest()
}
