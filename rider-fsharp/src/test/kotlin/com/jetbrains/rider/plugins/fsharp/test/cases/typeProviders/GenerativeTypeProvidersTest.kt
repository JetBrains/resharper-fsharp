package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.test.framework.dumpTypeProviders
import com.jetbrains.rider.projectView.solutionDirectoryPath
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.scriptingApi.waitForNextDaemon
import org.testng.annotations.Test
import java.time.Duration

@Solution("TypeProviderLibrary")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.FULL)
class GenerativeTypeProvidersTest : BaseTypeProvidersTest() {
  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun `generative type providers cross-project analysis`() {
    val generativeProviderProjectPath =
      "${project.solutionDirectoryPath}/GenerativeTypeProvider/GenerativeTypeProvider.fsproj"

    withOpenedEditor("GenerativeTypeLibrary/Library.fs") {
      waitForDaemon()
      markupAdapter.hasErrors.shouldBeTrue()

      buildSelectedProjectsWithReSharperBuild(listOf(generativeProviderProjectPath))

      waitForNextDaemon(Duration.ofSeconds(5))
      markupAdapter.hasErrors.shouldBeFalse()
    }

    unloadProject(arrayOf("TypeProviderLibrary", "GenerativeTypeProvider"))
    reloadProject(arrayOf("TypeProviderLibrary", "GenerativeTypeProvider"))

    withOpenedEditor("GenerativeTypeLibrary/Library.fs") {
      waitForDaemon()
      markupAdapter.hasErrors.shouldBeFalse()
    }
  }

  @Test
  @Mute("RIDER-111883", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun `change abbreviation`() {
    executeWithGold(testGoldFile) {
      withOpenedEditor("GenerativeTypeProvider/Library.fs") {
        waitForDaemon()

        it.println("Before:\n")
        dumpTypeProviders(it)

        // change abbreviation from "SimpleGenerativeTypeAbbr" to "SimpleGenerativeTypeAbbr1"
        typeFromOffset("1", 103)
        waitForDaemon()

        it.println("\n\nAfter:\n")
        dumpTypeProviders(it)
      }
    }
  }
}
