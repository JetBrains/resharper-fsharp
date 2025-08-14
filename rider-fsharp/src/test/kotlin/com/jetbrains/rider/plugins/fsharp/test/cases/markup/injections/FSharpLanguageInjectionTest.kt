package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.intellij.codeInsight.daemon.impl.HighlightInfoType
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.BaseTestWithMarkup
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.getHighlighters
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
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


@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("CoreConsoleApp")
class FSharpLanguageInjectionTest : BaseTestWithMarkup() {
  @Test
  fun testInjectionByCommentInRegularStrings() = doTest()

  @Test
  fun testInjectionByCommentInVerbatimStrings() = doTest()

  @Mute("RIDER-123576")
  @Test
  fun testInjectionByCommentInRegularInterpolatedStrings() = doTest()

  @Mute("RIDER-123576")
  @Test
  fun testInjectionByCommentInVerbatimInterpolatedStrings() = doTest()

  @Test
  fun testInjectionByCommentInTripleQuotedStrings() = doTest()

  //TODO: fix lexer for second case
  @Mute("RIDER-123576")
  @Test
  fun testInjectionByCommentInRawStrings() = doTest()

  @Mute("RIDER-123576")
  @Test
  fun testInjectionByCommentInTripleQuotedInterpolatedStrings() = doTest()

  @Mute("RIDER-123576")
  @Test
  fun testEscapeSequences() = doTest()

  @Mute("RIDER-123576")
  @Test
  fun testInjectionByAnnotation() = doTest()

  @Mute("RIDER-123576")
  @Test
  fun testInjectionByFunction() = doTest()
}

@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("FableApp")
class FSharpFrontendFrameworksTest : BaseTestWithMarkup() {
  @Test
  fun testInjectionByExternalAnnotation() = doTest()
}
