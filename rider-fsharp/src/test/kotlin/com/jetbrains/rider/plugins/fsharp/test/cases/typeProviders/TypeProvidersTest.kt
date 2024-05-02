package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.BuildTool
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_CORE_3_1, buildTool = BuildTool.FULL)
class TypeProvidersTest : BaseTypeProvidersTest() {
  override fun getSolutionDirectoryName() = "TypeProviderLibrary"

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
  @TestEnvironment(solution = "LegacyTypeProviderLibrary")
  fun legacyTypeProviders() = doTest("LegacyTypeProviders")

  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  @TestEnvironment(
    solution = "CsvTypeProvider",
    sdkVersion = SdkVersion.DOT_NET_6
  )
  fun `csvProvider - units of measure`() = doTest("Library")

  private fun doTest(fileName: String) {
    withOpenedEditor(project, "TypeProviderLibrary/$fileName.fs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }
  }
}
