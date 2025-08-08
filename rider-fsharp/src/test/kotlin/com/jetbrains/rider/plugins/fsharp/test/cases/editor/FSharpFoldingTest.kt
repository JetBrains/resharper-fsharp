package com.jetbrains.rider.plugins.fsharp.test.cases.editor

import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.CodeFoldingTestBase
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import org.testng.annotations.Test

@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE)
@Solution("CoreConsoleApp")
class FSharpFoldingTest : CodeFoldingTestBase() {

  @Test
  fun codeFolding() {
    doTestWithMarkupModel("CodeFolding.fs", "CodeFolding.fs") {
      waitForDaemon()
      dumpFoldingHighlightersWithText()
    }
  }
}
