package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.intellij.codeInsight.daemon.impl.HighlightInfoType
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.junit5.base.BaseTestWithMarkup
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.getHighlighters
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

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

@Tag(TeamCityTags.Plugins.FSharp)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
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

@Tag(TeamCityTags.Plugins.FSharp)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("FableApp")
class FSharpFrontendFrameworksTest : BaseTestWithMarkup() {
  @Test
  fun testInjectionByExternalAnnotation() = doTest()
}
