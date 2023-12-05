package com.jetbrains.rider.plugins.fsharp.test.cases.templates.net70

import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.templates.sdk.ClassLibProjectTemplateTestBase
import com.jetbrains.rider.test.base.templates.sdk.ConsoleAppProjectTemplateTestBase
import com.jetbrains.rider.test.base.templates.sdk.XUnitProjectTemplateTestBase
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.BuildTool
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.ProjectTemplates

@Suppress("unused")
@Mute("Unable to load project and obtain project information from MsBuild.", [PlatformType.LINUX_ARM64])
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_7, buildTool = BuildTool.SDK )
object Net70 {
  class ClassLibProjectTemplateTest : ClassLibProjectTemplateTestBase(ProjectTemplates.Sdk.Net7.FSharp.classLibrary) {
    override val expectedNumOfAnalyzedFiles: Int = 1
    override val expectedNumOfSkippedFiles: Int = 0
    override val targetFramework: String = "net7.0"
    override val buildFilesIgnoreList: Set<Regex> = setOf(
      Regex("ClassLibrary/bin/Debug/net7\\.0/ClassLibrary\\.deps\\.json"),
      Regex("ClassLibrary/obj/Debug/net7\\.0/ref(int)?/.*")
    )
    init {
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
    }
  }

  class ConsoleAppProjectTemplateTest : ConsoleAppProjectTemplateTestBase(ProjectTemplates.Sdk.Net7.FSharp.consoleApplication) {
    override val expectedNumOfAnalyzedFiles: Int = 3
    override val expectedNumOfSkippedFiles: Int = 0
    override val breakpointLine: Int = 2
    override val expectedOutput: String = "Hello from F#"
    override val debugFileName: String = "Program.fs"
    override val buildFilesIgnoreList: Set<Regex> = setOf(
      Regex("ConsoleApplication/bin/Debug/net7\\.0/FSharp\\.Core\\.dll"),
      Regex("ConsoleApplication/bin/Debug/net7\\.0/.*/FSharp\\.Core\\.resources\\.dll"),
      Regex("ConsoleApplication/(bin|obj)/Debug/net7\\.0/ConsoleApplication\\.(fsproj\\.CopyComplete|runtimeconfig\\.json|deps\\.json)"),
      Regex("ConsoleApplication/obj/Debug/net7\\.0/ref(int)?/.*")
    )

    init {
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
    }
  }

  class XUnitProjectTemplateTest : XUnitProjectTemplateTestBase(ProjectTemplates.Sdk.Net7.FSharp.xUnit) {
    override val expectedNumOfAnalyzedFiles: Int = 1
    override val expectedNumOfSkippedFiles: Int = 0
    override val sessionElements: Int = 3
    override val debugFileName: String = "Tests.fs"
    override val breakpointLine: Int = 8
    override val buildFilesIgnoreList: Set<Regex> = setOf(
      Regex("UnitTestProject/bin/Debug/net7\\.0/FSharp\\.Core\\.dll"),
      Regex("UnitTestProject/bin/Debug/net7\\.0/.*/.*\\.dll"), // Localization folders, like cs/de/es
      Regex("UnitTestProject/bin/Debug/net7\\.0/Microsoft\\.(TestPlatform|VisualStudio).*\\.dll"),
      Regex("UnitTestProject/bin/Debug/net7\\.0/(Newtonsoft\\.Json|NuGet\\.Frameworks|testhost|xunit\\.).*\\.(dll|exe)"),
      Regex("UnitTestProject/(bin|obj)/Debug/net7\\.0/UnitTestProject\\.(fsproj\\.CopyComplete|runtimeconfig\\.json|deps\\.json)"),
      Regex("UnitTestProject/obj/Debug/net7\\.0/ref(int)?/.*")
    )
    init {
      addMute(Mute("RIDER-102872"), ::createTemplateProject)
      addMute(Mute("No run configuration"), XUnitProjectTemplateTestBase::runConfiguration)
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
    }
  }
}
