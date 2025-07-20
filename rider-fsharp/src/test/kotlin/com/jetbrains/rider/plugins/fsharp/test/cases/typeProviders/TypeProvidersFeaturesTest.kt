package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.intellij.openapi.actionSystem.ActionPlaces
import com.intellij.openapi.actionSystem.IdeActions
import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.plugins.fsharp.logs.FSharpLogTraceScenarios
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.EditorTestBase
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.time.Duration

@Test
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class TypeProvidersFeaturesTest : EditorTestBase() {
  override val testSolution = "SwaggerProviderCSharp"
  override val restoreNuGetPackages = true
  override val traceScenarios: Set<LogTraceScenario>
    get() = super.traceScenarios + FSharpLogTraceScenarios.FSharpTypeProviders

  @Test
  fun `signature file navigation`() = doNavigationTestWithMultipleDeclarations()

  @Test
  fun `provided member navigation`() = doNavigationTest("SwaggerProvider.fs")

  @Test
  fun `provided abbreviation navigation`() = doNavigationTest("SwaggerProvider.fs")

  @Test
  fun `navigate to decompiled`() {
    withOpenedEditor("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
      waitForDaemon()
      callAction(IdeActions.ACTION_GOTO_SUPER)
      waitForEditorSwitch("ProvidedApiClientBase.cs")
    }
  }

  @Test
  fun `provided nested type navigation`() = doNavigationTest("SwaggerProvider.fs")

  @Test
  fun `multiply different abbreviation type parts - before`() = doNavigationTestWithMultipleDeclarations()

  @Test
  fun `multiply different abbreviation type parts - after`() = doNavigationTestWithMultipleDeclarations()

  @Test
  fun `provided member rename disabled`() = doRenameUnavailableTest()

  @Test
  fun `provided nested type rename disabled`() = doRenameUnavailableTest()

  @Suppress("SameParameterValue")
  private fun doNavigationTest(declarationFileName: String) {
    withOpenedEditor("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
      waitForDaemon()
      gotoDeclaration {
        waitForEditorSwitch(declarationFileName)
        waitForDaemon()
        executeWithGold(testGoldFile) {
          dumpOpenedDocument(it, project!!, true)
        }
      }
    }
  }

  private fun doNavigationTestWithMultipleDeclarations() {
    withOpenedEditor("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        callActionAndHandlePopup(IdeActions.ACTION_GOTO_DECLARATION, true, Duration.ofSeconds(1), it) {
          this.cancel()
        }
      }
    }
  }

  private fun doRenameUnavailableTest() {
    withOpenedEditor("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
      waitForDaemon()
      assertActionDisabled(project!!, dataContext, ActionPlaces.UNKNOWN, IdeActions.ACTION_RENAME)
    }
  }
}
