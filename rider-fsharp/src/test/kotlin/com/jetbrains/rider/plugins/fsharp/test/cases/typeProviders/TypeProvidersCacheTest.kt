package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rd.platform.diagnostics.RdLogTraceScenarios
import com.jetbrains.rider.plugins.fsharp.test.framework.dumpTypeProviders
import com.jetbrains.rider.plugins.fsharp.test.framework.flushFileChanges
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.Mono
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.disableTimedMarkupSuppression
import com.jetbrains.rider.test.framework.enableTimedMarkupSuppression
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File

@Solution("TypeProviderLibrary")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.FULL, mono = Mono.UNIX_ONLY)
class TypeProvidersCacheTest : BaseTypeProvidersTest() {
  private val defaultSourceFile = "TypeProviderLibrary/Caches.fs"

  override val traceScenarios: Set<LogTraceScenario>
    get() = super.traceScenarios + RdLogTraceScenarios.Daemon

  private fun checkTypeProviders(testGoldFile: File, sourceFile: String) {
    withOpenedEditor(sourceFile) {
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

    withOpenedEditor(defaultSourceFile) {
      disableTimedMarkupSuppression()
      waitForDaemon()

      flushFileChanges(project!!) {
        testDirectory.deleteRecursively().shouldBeTrue()
      }
      typeWithLatency("//")
      waitForNextDaemon()

      executeWithGold(File(testGoldFile.path + "_before")) {
        dumpTypeProviders(it)
      }

      flushFileChanges(project!!) {
        testDirectory.mkdir().shouldBeTrue()
      }
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
    withOpenedEditor(defaultSourceFile) {
      waitForDaemon()
      typeWithLatency("//")
      checkTypeProviders(testGoldFile, defaultSourceFile)
    }
  }

  @Test
  @Mute("RIDER-111885", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun projectsWithEqualProviders() {
    withOpenedEditor("TypeProviderLibrary/Library.fs") {
      waitForDaemon()
    }
    withOpenedEditor("TypeProviderLibrary2/Library.fs") {
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
