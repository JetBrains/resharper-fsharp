package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.junit5.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Tag(TeamCityTags.Plugins.FSharp)
@Solution("ReferencesOrder")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
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
