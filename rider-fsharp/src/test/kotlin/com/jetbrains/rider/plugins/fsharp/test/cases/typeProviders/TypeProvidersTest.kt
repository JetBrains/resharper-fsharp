package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.BuildTool
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_CORE_3_1, buildTool = BuildTool.FULL)
class TypeProvidersTest : BaseTestWithSolution() {
  override fun getSolutionDirectoryName() = "TypeProviderLibrary"
  override val restoreNuGetPackages = true

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

  private fun doTest(fileName: String) {
    withOpenedEditor(project, "TypeProviderLibrary/$fileName.fs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }
  }
}
