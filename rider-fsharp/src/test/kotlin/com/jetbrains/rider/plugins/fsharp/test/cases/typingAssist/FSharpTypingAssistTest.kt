package com.jetbrains.rider.plugins.fsharp.test.cases.typingAssist

import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.TypingAssistTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.dumpOpenedEditor
import com.jetbrains.rider.test.scriptingApi.typeOrCallAction
import org.testng.annotations.DataProvider
import org.testng.annotations.Test

@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
class FSharpTypingAssistTest : TypingAssistTestBase() {

  override val testSolution: String = "CoreConsoleApp"

  @DataProvider(name = "testTypingAssists")
  fun testTypingAssists() = arrayOf(
    arrayOf("closingBrace1", "{"),
    arrayOf("parentheses1", "("),
    arrayOf("quotes1", "\""),
    arrayOf("singleQuote1", "'"),
    arrayOf("closingAngleBracket1", "<"),
    arrayOf("indentAfterEnter", "EditorEnter")
  )

  @Test(dataProvider = "testTypingAssists")
  fun testTyping(caseName: String, input: String) {
    dumpOpenedEditor("Program.fs", "Program.fs") {
      typeOrCallAction(input)
    }
  }

}
