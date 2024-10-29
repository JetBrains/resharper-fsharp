package com.jetbrains.rider.plugins.fsharp.test.cases.markup

import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.plugins.fsharp.test.withCultureInfo
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldContains
import com.jetbrains.rider.test.base.DocumentationTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import org.testng.annotations.Test

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
@Solution("CoreConsoleApp")
class FSharpHoverDocTest : DocumentationTestBase() {
  override val checkSolutionLoad = false

  @Test
  fun `test hover docs for EntryPoint`() = doTest("Program.fs", "Program.fs")

  @Test
  fun `test hover docs for a function definition`() = doTest("Program.fs", "Program.fs")

  @Test
  fun `test hover docs for a parameter`() = doTest("Program.fs", "Program.fs")

  @Test
  fun `test hover docs for a submodule`() = doTest("Program.fs", "Program.fs")

  @Test
  fun `test xml doc with symbol reference`() = doTest("Program.fs", "Program.fs")

  @Test
  fun `test empty xml doc`() = doTest("Program.fs", "Program.fs")

  @Test
  fun `test hover docs for a shorthand`() = doTest("Program.fs", "Program.fs")

  @Mute("RIDER-103671")
  @Test
  @Solution("ConsoleAppTwoTargetFrameworks")
  fun `test multiple frameworks`() = doTest("Program.fs", "Program.fs")

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
  @Solution("SwaggerProviderCSharp")
  fun `provided method in csharp`() = doTest("CSharpLibrary.cs", "CSharpLibrary.cs")

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
  @Solution("SwaggerProviderCSharp")
  fun `provided abbreviation in csharp`() = doTestWithTypeProviders("OpenAPI Provider for")

  @Test
  fun `test xml doc parsing error`() {
    withCultureInfo(project, "en-US") {
      doTest("Program.fs", "Program.fs")
    }
  }

  @Suppress("SameParameterValue")
  private fun doTestWithTypeProviders(summary: String) {
    doTestWithMarkupModel("CSharpLibrary.cs", "CSharpLibrary.cs") {
      waitForDaemon()
      generateBackendHoverDoc().shouldContains(summary)
    }
  }
}
