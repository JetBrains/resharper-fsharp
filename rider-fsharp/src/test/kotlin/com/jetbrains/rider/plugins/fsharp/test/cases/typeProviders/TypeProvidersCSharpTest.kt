package com.jetbrains.rider.plugins.fsharp.test.cases.typeProviders

import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.callBasicCompletion
import com.jetbrains.rider.test.scriptingApi.defaultRefactoringRename
import com.jetbrains.rider.test.scriptingApi.dumpActiveLookupItemsPresentations
import com.jetbrains.rider.test.scriptingApi.dumpOpenedDocument
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.reloadAllProjects
import com.jetbrains.rider.test.scriptingApi.typeFromOffset
import com.jetbrains.rider.test.scriptingApi.unloadAllProjects
import com.jetbrains.rider.test.scriptingApi.waitForAllAnalysisFinished
import com.jetbrains.rider.test.scriptingApi.waitForCompletion
import com.jetbrains.rider.test.scriptingApi.waitForDaemon
import com.jetbrains.rider.test.scriptingApi.waitForNextDaemon
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
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
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_6, buildTool = BuildTool.SDK)
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
