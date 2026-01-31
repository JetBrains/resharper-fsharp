package com.jetbrains.rider.plugins.fsharp.test.cases.fantomas

import com.jetbrains.rider.plugins.fsharp.test.framework.withEditorConfig
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.EditorTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.dumpOpenedDocument
import com.jetbrains.rider.test.scriptingApi.reformatCode
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Test
@TestSettings(sdkVersion = SdkVersion.DOT_NET_6, buildTool = BuildTool.SDK)
class FantomasTest : EditorTestBase() {
  override val testSolution: String = "FormatCodeApp"

  @Mute("RIDER-114935", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  @Test
  fun withEditorConfig() = doTest("EditorConfig.fs")

  @Mute("RIDER-114935", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  @Test
  fun simpleFormatting() = doTest("Simple.fs")

  @Test
  fun formatLastFile() = doTest("Program.fs")

  private fun doTest(fileName: String) {
    withEditorConfig(project) {
      withOpenedEditor(fileName) {
        waitForDaemon()
        reformatCode()
        executeWithGold(testGoldFile) {
          dumpOpenedDocument(it, project!!, false)
        }
      }
    }
  }
}
