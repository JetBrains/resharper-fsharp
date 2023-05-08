package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.RiderSqlInjectionTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import org.testng.annotations.Test

@TestEnvironment(solution = "CoreConsoleApp", sdkVersion = SdkVersion.DOT_NET_6)
class FSharpSqlInjectionTest : RiderSqlInjectionTestBase() {
  @Test
  fun `test sql with missing parameter`() = doTest()

  @Test
  fun `test sql with parameter added with string_Format`() = doTest()

  @Test
  fun `test sql with parameter added with string_Format 2`() = doTest()

  @Test
  fun `test sql with parameter added with interpolation`() = doTest()

  //TODO: support identifiers in concatenations
  @Test
  fun `test sql with parameters added with different ways`() = doTest()

  @Test
  fun `test sql with a valid Dapper parameter`() = doTest()

  @Test
  fun `test sql with a valid sql command parameter`() = doTest()

  @Test
  fun `test escaped quotes in normal strings`() = doTest()

  @Test
  fun `test escaped quotes in interpolated strings`() = doTest()

  @Test
  fun `test escaped quotes in verbatim strings`() = doTest()

  @Test
  fun `test concatenation of regular strings`() = doTest()

  @Test
  fun `test concatenation of interpolated strings`() = doTest()

  @Test
  fun `test concatenation of verbatim strings`() = doTest()

  @Test
  fun `test concatenation of verbatim interpolated strings`() = doTest()

  @Test
  fun `test sql with parameter added with concatenation of a variable`() = doTest()
  @Test
  fun `test sql with quoted parameter in interpolated string`() = doTest()

  private fun doTest() = super.doTest("Program.fs", dumpInjections = true, dumpInspections = true) { }
}
