package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.test.fcsHost
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.BuildTool
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Test
class TypeProvidersRuntimeTest : BaseTypeProvidersTest() {
  override fun getSolutionDirectoryName() = "CoreTypeProviderLibrary"

  @Test
  @TestEnvironment(
    sdkVersion = SdkVersion.DOT_NET_CORE_3_1,
    buildTool = BuildTool.FULL,
    platform = [PlatformType.WINDOWS_ALL],
    solution = "TypeProviderLibrary"
  )
  fun framework461() = doTest(".NET Framework 4.8")

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_CORE_3_1)
  fun core31() = doTest(".NET Core 3.1")

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_5)
  fun net5() = doTest(".NET 5")

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
  fun net6() = doTest(".NET 6")

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_7)
  fun net7() = doTest(".NET 7")

  @Mute("RIDER-103648")
  @TestEnvironment(
    sdkVersion = SdkVersion.DOT_NET_CORE_3_1,
    solution = "FscTypeProviderLibrary"
  )
  fun fsc() = doTest(".NET Framework 4.8")

  private fun doTest(expectedRuntime: String) {
    withOpenedEditor(project, "TypeProviderLibrary/Library.fs") {
      waitForDaemon()
      val typeProvidersRuntimeVersion = this.project!!.fcsHost.typeProvidersRuntimeVersion.sync(Unit)
      typeProvidersRuntimeVersion
        .shouldNotBeNull()
        .startsWith(expectedRuntime)
        .shouldBeTrue("'$typeProvidersRuntimeVersion' should start with '$expectedRuntime'")
      markupAdapter.hasErrors.shouldBeFalse()
    }
  }
}
