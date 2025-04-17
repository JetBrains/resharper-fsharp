package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.scriptingApi.waitForNextDaemon
import org.testng.annotations.Test
import java.io.File

@Solution("YamlProviderCSharp")
class TypeProvidersCSharpTest : BaseTypeProvidersTest() {
  @Test
  fun resolveTest() {
    withOpenedEditor("CSharpLibrary/CSharpLibrary.cs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }

    unloadAllProjects()
    reloadAllProjects(project)

    withOpenedEditor("CSharpLibrary/CSharpLibrary.cs") {
      waitForDaemon()
      executeWithGold(testGoldFile) {
        dumpSevereHighlighters(it)
      }
    }
  }

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
  @Solution("SwaggerProviderCSharp")
  fun changeStaticArg() {
    withOpenedEditor("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
      waitForDaemon()
      markupAdapter.hasErrors.shouldBeFalse()
    }

    withOpenedEditor("SwaggerProviderLibrary/Literals.fs") {
      waitForDaemon()
      // change schema path from "specification.json" to "specification1.json"
      typeFromOffset("1", 86)
    }

    withOpenedEditor("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
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

  @Mute("RIDER-117704")
  @Test
  @Solution("YamlProviderCSharp")
  fun `provided type abbreviation completion`() =
    doTestDumpLookupItems("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.fs")

  @Mute("RIDER-117704")
  @Test
  @Solution("YamlProviderCSharp")
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
