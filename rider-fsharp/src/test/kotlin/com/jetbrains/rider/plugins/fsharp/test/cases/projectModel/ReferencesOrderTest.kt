package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import org.testng.annotations.Test

@Solution("ReferencesOrder")
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class ReferencesOrder : PerTestSolutionTestBase() {
  override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
    params.waitForCaches = true
    params.restoreNuGetPackages = true
  }

  @Test
  fun testReferencesOrder() {
    val references = project.fcsHost.dumpSingleProjectLocalReferences.sync(Unit)
    assert(references == listOf("Library1.dll", "Library2.dll"))
  }
}
