package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.plugins.fsharp.test.dumpTypeProviders
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.env.enums.BuildTool
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.reloadAllProjects
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.unloadAllProjects
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.waitForDaemon
import org.testng.annotations.Test
import java.io.File

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_CORE_3_1, buildTool = BuildTool.FULL)
class TypeProvidersCacheTest : BaseTypeProvidersTest() {
  override fun getSolutionDirectoryName() = "TypeProviderLibrary"
  private val defaultSourceFile = "TypeProviderLibrary/Caches.fs"

  private fun checkTypeProviders(testGoldFile: File, sourceFile: String) {
    withOpenedEditor(project, sourceFile) {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpTypeProviders(it)
      }
    }
  }

  @Test
  fun checkCachesWhenProjectReloading() {
    checkTypeProviders(File(testGoldFile.path + "_before"), defaultSourceFile)

    unloadAllProjects()
    reloadAllProjects(project)

    checkTypeProviders(File(testGoldFile.path + "_after"), defaultSourceFile)
  }

  @Test
  fun invalidation() {
    val testDirectory = File(project.basePath + "/TypeProviderLibrary/Test")

    withOpenedEditor(project, defaultSourceFile) {
      waitForDaemon()

      testDirectory.deleteRecursively().shouldBeTrue()
      typeWithLatency("//")
      waitForDaemon()

      executeWithGold(File(testGoldFile.path + "_before")) {
        dumpTypeProviders(it)
      }

      testDirectory.mkdir().shouldBeTrue()
      typeWithLatency(" ")
      waitForDaemon()

      executeWithGold(File(testGoldFile.path + "_after")) {
        dumpTypeProviders(it)
      }
    }
  }

  @Test
  fun typing() {
    withOpenedEditor(project, defaultSourceFile) {
      waitForDaemon()
      typeWithLatency("//")
      checkTypeProviders(testGoldFile, defaultSourceFile)
    }
  }

  @Test
  fun projectsWithEqualProviders() {
    withOpenedEditor(project, "TypeProviderLibrary/Library.fs") {
      waitForDaemon()
    }
    withOpenedEditor(project, "TypeProviderLibrary2/Library.fs") {
      waitForDaemon()
      checkTypeProviders(testGoldFile, defaultSourceFile)
    }
  }

  @Test(description = "RIDER-73091", enabled = false)
  fun script() {
    checkTypeProviders(File(testGoldFile.path + "_before"), "TypeProviderLibrary/Script.fsx")

    unloadAllProjects()
    reloadAllProjects(project)

    checkTypeProviders(File(testGoldFile.path + "_after"), "TypeProviderLibrary/Script.fsx")
  }
}
