package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rd.platform.diagnostics.RdLogTraceScenarios
import com.jetbrains.rider.plugins.fsharp.test.dumpTypeProviders
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.BuildTool
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.disableTimedMarkupSuppression
import com.jetbrains.rider.test.framework.enableTimedMarkupSuppression
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.reloadAllProjects
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.unloadAllProjects
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.waitForDaemon
import com.jetbrains.rider.test.waitForNextDaemon
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_CORE_3_1, buildTool = BuildTool.FULL)
class TypeProvidersCacheTest : BaseTypeProvidersTest() {
  override val testSolution = "TypeProviderLibrary"
  private val defaultSourceFile = "TypeProviderLibrary/Caches.fs"

  override val traceScenarios: Set<LogTraceScenario>
    get() = super.traceScenarios + RdLogTraceScenarios.Daemon

  private fun checkTypeProviders(testGoldFile: File, sourceFile: String) {
    withOpenedEditor(project, sourceFile) {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpTypeProviders(it)
      }
    }
  }

  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun checkCachesWhenProjectReloading() {
    checkTypeProviders(File(testGoldFile.path + "_before"), defaultSourceFile)

    unloadAllProjects()
    reloadAllProjects(project)

    checkTypeProviders(File(testGoldFile.path + "_after"), defaultSourceFile)
  }

  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun invalidation() {
    val testDirectory = File(project.basePath + "/TypeProviderLibrary/Test")

    withOpenedEditor(project, defaultSourceFile) {
      disableTimedMarkupSuppression()
      waitForDaemon()

      testDirectory.deleteRecursively().shouldBeTrue()
      typeWithLatency("//")
      waitForNextDaemon()

      executeWithGold(File(testGoldFile.path + "_before")) {
        dumpTypeProviders(it)
      }

      testDirectory.mkdir().shouldBeTrue()
      typeWithLatency(" ")
      waitForNextDaemon()

      executeWithGold(File(testGoldFile.path + "_after")) {
        dumpTypeProviders(it)
      }

      enableTimedMarkupSuppression()
    }
  }

  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun typing() {
    withOpenedEditor(project, defaultSourceFile) {
      waitForDaemon()
      typeWithLatency("//")
      checkTypeProviders(testGoldFile, defaultSourceFile)
    }
  }

  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun projectsWithEqualProviders() {
    withOpenedEditor(project, "TypeProviderLibrary/Library.fs") {
      waitForDaemon()
    }
    withOpenedEditor(project, "TypeProviderLibrary2/Library.fs") {
      waitForDaemon()
      checkTypeProviders(testGoldFile, defaultSourceFile)
    }
  }

  @Mute("RIDER-103648")
  @Test(description = "RIDER-73091")
  fun script() {
    checkTypeProviders(File(testGoldFile.path + "_before"), "TypeProviderLibrary/Script.fsx")

    unloadAllProjects()
    reloadAllProjects(project)

    checkTypeProviders(File(testGoldFile.path + "_after"), "TypeProviderLibrary/Script.fsx")
  }
}
