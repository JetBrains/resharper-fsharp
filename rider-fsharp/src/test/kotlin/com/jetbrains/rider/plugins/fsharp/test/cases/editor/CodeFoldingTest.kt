package com.jetbrains.rider.plugins.fsharp.test.cases.editor

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CodeFoldingTestBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@TestEnvironment(solution = "CoreConsoleApp", coreVersion = CoreVersion.LATEST_STABLE)
class CodeFoldingTest : CodeFoldingTestBase() {

  @Test
  fun codeFolding() {
    doTestWithMarkupModel("CodeFolding.fs", "CodeFolding.fs") {
      waitForDaemon()
      dumpFoldingHighlightersWithText()
    }
  }
}
