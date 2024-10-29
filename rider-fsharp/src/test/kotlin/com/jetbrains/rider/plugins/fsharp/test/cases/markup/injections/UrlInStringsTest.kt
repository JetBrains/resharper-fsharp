package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithMarkup
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.getHighlighters
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import org.testng.annotations.Test

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_7)
@Solution("CoreConsoleApp")
class UrlInStringsTest : BaseTestWithMarkup() {
  private fun doTest() {
    doTestWithMarkupModel("Program.fs", "Program.fs") {
      waitForDaemon()
      printStream.print(getHighlighters(project!!, this) { true })
    }
  }

  fun simple() = doTest()
}
