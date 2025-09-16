package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.RiderSqlInjectionTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import org.testng.annotations.Test

@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("CoreConsoleApp")
class FSharpSqlAutomaticInjectionTest : RiderSqlInjectionTestBase() {

  @Test
  fun `test auto injections`() = doTest()

  private fun doTest() = super.doTest("Program.fs", dumpInjections = true, dumpInspections = false) { }
}
