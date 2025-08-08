package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.BaseTestWithMarkup
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.getHighlighters
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import org.testng.annotations.Test

@Test
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE)
@Solution("CoreConsoleApp")
class FSharpUrlInStringsTest : BaseTestWithMarkup() {
  private fun doTest() {
    doTestWithMarkupModel("Program.fs", "Program.fs") {
      waitForDaemon()
      printStream.print(getHighlighters(project!!, this) { true })
    }
  }

  @Test
  fun simple() = doTest()
}
