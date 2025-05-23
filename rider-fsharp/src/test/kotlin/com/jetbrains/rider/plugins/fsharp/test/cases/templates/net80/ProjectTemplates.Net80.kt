package com.jetbrains.rider.plugins.fsharp.test.cases.templates.net80

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
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_8, buildTool = BuildTool.SDK, platform = [PlatformType.WINDOWS_X64, PlatformType.MAC_OS_ALL, PlatformType.LINUX_X64])
object Net80 {
  class ClassLibProjectTemplateTest : ClassLibProjectTemplateTestBase(ProjectTemplates.Sdk.Net8.FSharp.classLibrary) {
    override val targetFramework: String = "net8.0"
    override val buildFilesIgnoreList: Set<Regex> = setOf(
      Regex("ClassLibrary/bin/Debug/net8\\.0/ClassLibrary\\.deps\\.json"),
      Regex("ClassLibrary/obj/Debug/net8\\.0/ref(int)?/.*")
    )

    init {
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
    }
  }

  class ConsoleAppProjectTemplateTest : ConsoleAppProjectTemplateTestBase(ProjectTemplates.Sdk.Net8.FSharp.consoleApplication) {
    override val breakpointLine: Int = 2
    override val expectedOutput: String = "Hello from F#"
    override val debugFileName: String = "Program.fs"
    override val buildFilesIgnoreList: Set<Regex> = setOf(
      Regex("ConsoleApplication/bin/Debug/net8\\.0/FSharp\\.Core\\.dll"),
      Regex("ConsoleApplication/bin/Debug/net8\\.0/.*/FSharp\\.Core\\.resources\\.dll"),
      Regex("ConsoleApplication/(bin|obj)/Debug/net8\\.0/ConsoleApplication\\.(fsproj\\.CopyComplete|runtimeconfig\\.json|deps\\.json)"),
      Regex("ConsoleApplication/obj/Debug/net8\\.0/ref(int)?/.*")
    )

    init {
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
      addMute(Mute("RIDER-117187"), ::debugProgram)
    }
  }

  class XUnitProjectTemplateTest : XUnitProjectTemplateTestBase(ProjectTemplates.Sdk.Net8.FSharp.xUnit) {
    override val sessionElements: Int = 3
    override val debugFileName: String = "Tests.fs"
    override val breakpointLine: Int = 8
    override val buildFilesIgnoreList: Set<Regex> = setOf(
      Regex("UnitTestProject/bin/Debug/net8\\.0/FSharp\\.Core\\.dll"),
      Regex("UnitTestProject/bin/Debug/net8\\.0/.*/.*\\.dll"), // Localization folders, like cs/de/es
      Regex("UnitTestProject/bin/Debug/net8\\.0/Microsoft\\.(TestPlatform|VisualStudio).*\\.dll"),
      Regex("UnitTestProject/bin/Debug/net8\\.0/(Newtonsoft\\.Json|NuGet\\.Frameworks|testhost|xunit\\.).*\\.(dll|exe)"),
      Regex("UnitTestProject/(bin|obj)/Debug/net8\\.0/UnitTestProject\\.(fsproj\\.CopyComplete|runtimeconfig\\.json|deps\\.json)"),
      Regex("UnitTestProject/obj/Debug/net8\\.0/ref(int)?/.*")
    )

    init {
      addMute(Mute("RIDER-102872"), ::createTemplateProject)
      addMute(Mute("No run configuration"), testMethod = XUnitProjectTemplateTestBase::runConfiguration)
      addMute(Mute("RIDER-79065: No SWEA for F#"), ::swea)
    }
  }
}
