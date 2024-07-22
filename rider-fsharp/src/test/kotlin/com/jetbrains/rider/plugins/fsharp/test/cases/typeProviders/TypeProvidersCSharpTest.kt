package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.waitForDaemon
import com.jetbrains.rider.test.waitForNextDaemon
import org.testng.annotations.Test
import java.io.File

@Test
class TypeProvidersCSharpTest : BaseTypeProvidersTest() {
  override val testSolution = "YamlProviderCSharp"

  @Test
  fun resolveTest() {
    withOpenedEditor(project, "CSharpLibrary/CSharpLibrary.cs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }

    unloadAllProjects()
    reloadAllProjects(project)

    withOpenedEditor(project, "CSharpLibrary/CSharpLibrary.cs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }
  }

  @Test
  @TestEnvironment(
    solution = "SwaggerProviderCSharp",
    sdkVersion = SdkVersion.DOT_NET_6
  )
  fun changeStaticArg() {
    withOpenedEditor(project, "CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
      waitForDaemon()
      markupAdapter.hasErrors.shouldBeFalse()
    }

    withOpenedEditor(project, "SwaggerProviderLibrary/Literals.fs") {
      waitForDaemon()
      // change schema path from "specification.json" to "specification1.json"
      typeFromOffset("1", 86)
    }

    withOpenedEditor(project, "CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
      waitForNextDaemon()
      executeWithGold(File(testGoldFile.path + "_before")) {
        dumpSevereHighlighters(it)
      }

      // change method call from "ApiCoursesGet" to "ApiCoursesGet1"
      typeFromOffset("1", 195)
      waitForAllAnalysisFinished(project!!)

      executeWithGold(File(testGoldFile.path + "_after")) {
        dumpSevereHighlighters(it)
      }
    }
  }

  @Test
  fun `provided abbreviation rename`() {
    withOpenedEditor("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
      waitForDaemon()
      defaultRefactoringRename("Renamed")
      waitForNextDaemon()
      markupAdapter.hasErrors.shouldBeFalse()
      executeWithGold(File(testGoldFile.path + " - csharp")) {
        dumpOpenedDocument(it, project!!)
      }
    }

    withOpenedEditor("YamlProviderLibrary/Library.fs") {
      waitForDaemon()
      markupAdapter.hasErrors.shouldBeFalse()
      executeWithGold(File(testGoldFile.path + " - fsharp")) {
        dumpOpenedDocument(it, project!!)
      }
    }
  }

  @Test
  @TestEnvironment(solution = "YamlProviderCSharp")
  fun `provided type abbreviation completion`() =
    doTestDumpLookupItems("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.fs")

  @Test
  @TestEnvironment(solution = "YamlProviderCSharp")
  fun `provided nested type completion`() = doTestDumpLookupItems("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.fs")

  @Suppress("SameParameterValue")
  private fun doTestDumpLookupItems(relativePath: String, sourceFileName: String) {
    withOpenedEditor(relativePath, sourceFileName) {
      waitForDaemon()
      callBasicCompletion()
      waitForCompletion()
      executeWithGold(testGoldFile) {
        dumpActiveLookupItemsPresentations(it)
      }
    }
  }
}
