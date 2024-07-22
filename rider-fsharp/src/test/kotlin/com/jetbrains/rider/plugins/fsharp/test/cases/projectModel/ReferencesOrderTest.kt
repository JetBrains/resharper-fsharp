package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.jetbrains.rider.plugins.fsharp.test.fcsHost
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import org.testng.annotations.Test

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class ReferencesOrder : BaseTestWithSolution() {
  override val testSolution = "ReferencesOrder"

  override val waitForCaches = true
  override val restoreNuGetPackages = true

  @Test()
  fun testReferencesOrder() {
    val references = project.fcsHost.dumpSingleProjectLocalReferences.sync(Unit)
    assert(references == listOf("Library1.dll", "Library2.dll"))
  }
}
