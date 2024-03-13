package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
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
  fun swaggerProvider() = doTest("SwaggerProvider")

  @Test
  fun simpleErasedProvider() = doTest("SimpleErasedProvider")

  @Test
  fun simpleGenerativeProvider() = doTest("SimpleGenerativeProvider")

  @Test
  fun providersErrors() = doTest("ProvidersErrors")

  @Test(description = "RIDER-60909")
  @TestEnvironment(solution = "LegacyTypeProviderLibrary")
  fun legacyTypeProviders() = doTest("LegacyTypeProviders")

  @Test
  @TestEnvironment(
    solution = "CsvTypeProvider",
    sdkVersion = SdkVersion.DOT_NET_6
  )
  fun `csvProvider - units of measure`() = doTest("Library")

  @Test(description = "RIDER-101544")
  @TestEnvironment(
    solution = "SwaggerProviderCSharp",
    sdkVersion = SdkVersion.DOT_NET_6
  )
  fun `srtp analysis`() {
    withOpenedEditor(project, "SwaggerProviderLibrary/SwaggerProvider.fs", "SwaggerProvider.fs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }
  }

  private fun doTest(fileName: String) {
    withOpenedEditor(project, "TypeProviderLibrary/$fileName.fs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }
  }
}
