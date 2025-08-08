package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Solution("CoreTypeProviderLibrary")
class TypeProvidersRuntimeTest : BaseTypeProvidersTest() {
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.FULL)
  @TestEnvironment(platform = [PlatformType.WINDOWS_ALL])
  @Solution("TypeProviderLibrary")
  fun framework461() = doTest(".NET Framework 4.8")

  @Test
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_CORE_3_1)
  fun core31() = doTest(".NET Core 3.1")

  @Test
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_5)
  fun net5() = doTest(".NET 5")

  @Test
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_6)
  fun net6() = doTest(".NET 6")

  @Test
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_7)
  fun net7() = doTest(".NET 7")

  @Test
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_9)
  fun net9() = doTest(".NET 9")

  @Mute("RIDER-103648")
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE)
  @Solution("FscTypeProviderLibrary")
  fun fsc() = doTest(".NET Framework 4.8")

  private fun doTest(expectedRuntime: String) {
    withOpenedEditor("TypeProviderLibrary/Library.fs") {
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
