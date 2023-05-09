package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.RiderSqlInjectionTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import org.testng.annotations.Test

@TestEnvironment(solution = "CoreConsoleApp", sdkVersion = SdkVersion.DOT_NET_6)
class FSharpSqlAutomaticInjectionTest : RiderSqlInjectionTestBase() {
  @Test
  fun `test that sql can be injected by comment`() = doTest()

  @Test
  fun `test that sql gets injected in verbatim string`() = doTest()

  @Test
  fun `test that sql gets injected in interpolated string`() = doTest()

  @Test
  fun `test that sql gets injected in verbatim interpolated string`() = doTest()

  @Test
  fun `test that sql gets injected in concatenation`() = doTest()

  @Test
  fun `test that sql gets injected in complex concatenation`() = doTest()

  @Test
  fun `test injection in split regular string`() = doTest()

  @Test
  fun `test injection in split interpolated string`() = doTest()

  @Test
  fun `test injection in split verbatim string`() = doTest()

  @Test
  fun `test injection in split verbatim interpolated string`() = doTest()

  @Test
  fun `test two injections in one statement`() = doTest()

  private fun doTest() = super.doTest("Program.fs", dumpInjections = true, dumpInspections = false) { }
}
