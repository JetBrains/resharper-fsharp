package com.jetbrains.rider.plugins.fsharp.test.cases.templates.core31

import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.templates.sdk.ClassLibProjectTemplateTestBase
import com.jetbrains.rider.test.base.templates.sdk.ConsoleAppProjectTemplateTestBase
import com.jetbrains.rider.test.base.templates.sdk.XUnitProjectTemplateTestBase
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.ProjectTemplateIds
import com.jetbrains.rider.test.scriptingApi.TemplateIdWithVersion


@Suppress("unused")
@TestEnvironment(
  sdkVersion = SdkVersion.DOT_NET_CORE_3_1,
  platform = [PlatformType.WINDOWS_X64, PlatformType.MAC_OS_ALL]
)
object Core31 {
  class ClassLibProjectTemplateTest : ClassLibProjectTemplateTestBase() {
    override val templateId: TemplateIdWithVersion
      get() = ProjectTemplateIds.currentSdk.fsharp_classLibrary
    override val expectedNumOfAnalyzedFiles: Int = 1
    override val expectedNumOfSkippedFiles: Int = 0
    override val targetFramework: String = "netcoreapp3.1"

    init {
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
    }
  }

  class ConsoleAppProjectTemplateTest : ConsoleAppProjectTemplateTestBase() {
    override val templateId: TemplateIdWithVersion
      get() = ProjectTemplateIds.currentSdk.fsharp_consoleApplication
    override val expectedNumOfAnalyzedFiles: Int = 0
    override val expectedNumOfSkippedFiles: Int = 3
    override val breakpointLine: Int = 7
    override val expectedOutput: String = "Hello World from F#!"
    override val debugFileName: String = "Program.fs"

    init {
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
    }
  }

  class XUnitProjectTemplateTest : XUnitProjectTemplateTestBase() {
    override val templateId: TemplateIdWithVersion
      get() = ProjectTemplateIds.currentSdk.fsharp_xUnit
    override val expectedNumOfAnalyzedFiles: Int = 1
    override val expectedNumOfSkippedFiles: Int = 0
    override val sessionElements: Int = 3
    override val debugFileName: String = "Tests.fs"
    override val breakpointLine: Int = 8

    init {
      addMute(Mute("No run configuration"), ::runConfiguration)
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
    }
  }
}