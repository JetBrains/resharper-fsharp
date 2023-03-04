package com.jetbrains.rider.plugins.fsharp.test.cases.templates.net60

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
@Mute("Unable to load project and obtain project information from MsBuild.", [PlatformType.LINUX_ARM64])
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
object Net60 {
  class ClassLibProjectTemplateTest : ClassLibProjectTemplateTestBase() {
    override val templateId: TemplateIdWithVersion
      get() = ProjectTemplateIds.currentSdk.fsharp_classLibrary
    override val expectedNumOfAnalyzedFiles: Int = 1
    override val expectedNumOfSkippedFiles: Int = 0
    override val targetFramework: String = "net6.0"

    init {
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
    }
  }

  class ConsoleAppProjectTemplateTest : ConsoleAppProjectTemplateTestBase() {
    override val templateId: TemplateIdWithVersion
      get() = ProjectTemplateIds.currentSdk.fsharp_consoleApplication
    override val expectedNumOfAnalyzedFiles: Int = 3
    override val expectedNumOfSkippedFiles: Int = 0
    override val breakpointLine: Int = 2
    override val expectedOutput: String = "Hello from F#"
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