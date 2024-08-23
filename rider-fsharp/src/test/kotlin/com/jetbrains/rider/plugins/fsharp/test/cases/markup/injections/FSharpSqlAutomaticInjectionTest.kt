package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.RiderSqlInjectionTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import org.testng.annotations.Test

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
@Solution("CoreConsoleApp")
class FSharpSqlAutomaticInjectionTest : RiderSqlInjectionTestBase() {
  @Test
  fun `test auto injections`() = doTest()

  private fun doTest() = super.doTest("Program.fs", dumpInjections = true, dumpInspections = false) { }
}
