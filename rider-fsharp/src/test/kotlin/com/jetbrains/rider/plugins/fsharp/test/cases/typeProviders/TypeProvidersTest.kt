package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.Mono
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Solution("TypeProviderLibrary")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.FULL, mono = Mono.UNIX_ONLY)
class TypeProvidersTest : BaseTypeProvidersTest() {
  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun swaggerProvider() = doTest("SwaggerProvider")

  @Test
  fun simpleErasedProvider() = doTest("SimpleErasedProvider")

  @Test
  fun simpleGenerativeProvider() = doTest("SimpleGenerativeProvider")

  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun providersErrors() = doTest("ProvidersErrors")

  @Test(description = "RIDER-60909")
  @Solution("LegacyTypeProviderLibrary")
  fun legacyTypeProviders() = doTest("LegacyTypeProviders")

  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  @TestSettings(
    sdkVersion = SdkVersion.LATEST_STABLE,
    buildTool = BuildTool.SDK
  )
  @Solution("CsvTypeProvider")
  fun `csvProvider - units of measure`() = doTest("Library")

  @Test(description = "RIDER-101544")
  @TestSettings(
    sdkVersion = SdkVersion.LATEST_STABLE,
    buildTool = BuildTool.SDK
  )
  @Solution("SwaggerProviderCSharp")
  fun `srtp analysis`() {
    withOpenedEditor("SwaggerProviderLibrary/SwaggerProvider.fs", "SwaggerProvider.fs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }
  }

  private fun doTest(fileName: String) {
    withOpenedEditor("TypeProviderLibrary/$fileName.fs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }
  }
}
