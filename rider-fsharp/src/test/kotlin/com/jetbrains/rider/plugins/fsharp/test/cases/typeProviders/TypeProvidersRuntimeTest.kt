package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.Mono
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Solution("CoreTypeProviderLibrary")
class TypeProvidersRuntimeTest : BaseTypeProvidersTest() {
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.FULL, mono = Mono.UNIX_ONLY)
  @TestEnvironment(platform = [PlatformType.WINDOWS_ALL])
  @Solution("TypeProviderLibrary")
  fun framework461() = doTest(".NET Framework 4.8")

  @Test
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_8, buildTool = BuildTool.SDK)
  fun net8() = doTest(".NET 8")

  @Test
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_9, buildTool = BuildTool.SDK)
  fun net9() = doTest(".NET 9")

  @Test
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_10, buildTool = BuildTool.SDK)
  fun net10() = doTest(".NET 10")

  @Test
  @Mute("RIDER-103648")
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
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
