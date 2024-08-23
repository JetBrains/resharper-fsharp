package com.jetbrains.rider.plugins.fsharp.test.cases.editor

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CodeFoldingTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import org.testng.annotations.Test

@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
@Solution("CoreConsoleApp")
class CodeFoldingTest : CodeFoldingTestBase() {

  @Test
  fun codeFolding() {
    doTestWithMarkupModel("CodeFolding.fs", "CodeFolding.fs") {
      waitForDaemon()
      dumpFoldingHighlightersWithText()
    }
  }
}
