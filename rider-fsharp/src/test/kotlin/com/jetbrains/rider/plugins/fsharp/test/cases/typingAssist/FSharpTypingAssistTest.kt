package com.jetbrains.rider.plugins.fsharp.test.cases.typingAssist

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.junit5.base.TypingAssistTestBase
import com.jetbrains.rider.test.scriptingApi.dumpOpenedEditor
import com.jetbrains.rider.test.scriptingApi.typeOrCallAction
import com.jetbrains.rider.test.suplementary.RiderTestSolution
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.params.ParameterizedTest
import org.junit.jupiter.params.provider.Arguments
import org.junit.jupiter.params.provider.MethodSource
import java.util.stream.Stream

@Tag(TeamCityTags.Plugins.FSharp)
@Solution("CoreConsoleApp")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
class FSharpTypingAssistTest : TypingAssistTestBase() {

  fun testTypingAssists(): Stream<Arguments> = Stream.of(
    Arguments.of("closingBrace1", "{"),
    Arguments.of("parentheses1", "("),
    Arguments.of("quotes1", "\""),
    Arguments.of("singleQuote1", "'"),
    Arguments.of("closingAngleBracket1", "<"),
    Arguments.of("indentAfterEnter", "EditorEnter")
  )

  @ParameterizedTest(name = "{0}")
  @MethodSource("testTypingAssists")
  fun testTyping(caseName: String, input: String) {
    dumpOpenedEditor("Program.fs", "Program.fs") {
      typeOrCallAction(input)
    }
  }

}
