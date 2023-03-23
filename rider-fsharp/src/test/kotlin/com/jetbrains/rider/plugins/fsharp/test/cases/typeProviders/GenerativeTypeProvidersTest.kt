package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.test.dumpTypeProviders
import com.jetbrains.rider.projectView.solutionDirectoryPath
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.BuildTool
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.waitForDaemon
import com.jetbrains.rider.test.waitForNextDaemon
import org.testng.annotations.Test
import java.time.Duration

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_CORE_3_1, buildTool = BuildTool.FULL)
class GenerativeTypeProvidersTest : BaseTestWithSolution() {
  override fun getSolutionDirectoryName() = "TypeProviderLibrary"

  @Test
  fun `generative type providers cross-project analysis`() {
    val generativeProviderProjectPath =
      "${project.solutionDirectoryPath}/GenerativeTypeProvider/GenerativeTypeProvider.fsproj"

    withOpenedEditor(project, "GenerativeTypeLibrary/Library.fs") {
      waitForDaemon()
      markupAdapter.hasErrors.shouldBeTrue()

      buildSelectedProjectsWithReSharperBuild(listOf(generativeProviderProjectPath))

      waitForNextDaemon(Duration.ofSeconds(5))
      markupAdapter.hasErrors.shouldBeFalse()
    }

    unloadProject(arrayOf("TypeProviderLibrary", "GenerativeTypeProvider"))
    reloadProject(arrayOf("TypeProviderLibrary", "GenerativeTypeProvider"))

    withOpenedEditor(project, "GenerativeTypeLibrary/Library.fs") {
      waitForDaemon()
      markupAdapter.hasErrors.shouldBeFalse()
    }
  }

  @Test
  fun `change abbreviation`() {
    executeWithGold(testGoldFile) {
      withOpenedEditor(project, "GenerativeTypeProvider/Library.fs") {
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
