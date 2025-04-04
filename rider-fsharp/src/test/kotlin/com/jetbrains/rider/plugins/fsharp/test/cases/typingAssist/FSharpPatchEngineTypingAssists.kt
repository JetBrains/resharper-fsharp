package com.jetbrains.rider.plugins.fsharp.test.cases.typingAssist

import com.intellij.openapi.actionSystem.IdeActions
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.PatchEngineEditorTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.logging.TestLoggerHelper
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.changeFileContent
import com.jetbrains.rider.test.scriptingApi.dumpOpenedEditorFacade
import com.jetbrains.rider.test.scriptingApi.withOpenedEditorFacade
import org.testng.annotations.DataProvider
import org.testng.annotations.Test

abstract class FSharpTypingAssistPatchEngineTest(mode: PatchEngineEditorTestMode) : PatchEngineEditorTestBase(mode) {
  override val checkTextControls = false
  override val testSolution = "CoreConsoleApp"

  @DataProvider(name = "simpleCases")
  fun simpleCases() = arrayOf(
    arrayOf("emptyFile"),
    arrayOf("docComment"),
    arrayOf("indent1"),
    arrayOf("indent2"),
    arrayOf("removeSelection1"),
    arrayOf("removeSelection2"),
    arrayOf("trailingSpace")
  )

  @Test(dataProvider = "simpleCases")
  fun testStartNewLine(caseName: String) {
    dumpOpenedEditorFacade("Program.fs", "Program.fs") {
      typeOrCallAction(IdeActions.ACTION_EDITOR_START_NEW_LINE)
    }
  }

  @Test
  fun testStartNewLineUndo() {
    dumpOpenedEditorFacade("Program.fs", "Program.fs") {
      typeOrCallAction(IdeActions.ACTION_EDITOR_START_NEW_LINE)
      undo()
    }
  }
}

@Test
@Subsystem(SubsystemConstants.TYPING_ASSIST)
@Feature("Typing Assist")
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FSharpTypingAssistPatchEngineSpeculativeAndForceRebaseTest :
  FSharpTypingAssistPatchEngineTest(PatchEngineEditorTestMode.SpeculativeAndForceRebase)


@Test
@Subsystem(SubsystemConstants.TYPING_ASSIST)
@Feature("Typing Assist")
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
abstract class FSharpBackendSyncTypingAssistTestBase(private val ideAction: String) :
  PatchEngineEditorTestBase(PatchEngineEditorTestMode.SpeculativeRebaseProhibited) {
  override val checkTextControls = false
  override val testSolution = "CoreConsoleApp"
  override val testDataDirectory
    get() = testDataStorage.testDataDirectory.resolve("../../../../ReSharper.FSharp/test/data/features/service/typingAssist")
  override val testCaseSourceDirectory
    get() = activeSolutionDirectory

  private val backendCases
    get() =
      testDataDirectory.listFiles()
        .filter { it.extension == "fs" && isApplicable(it.nameWithoutExtension) }
        .map { it.nameWithoutExtension }


  @DataProvider(name = "supportedBackendCases")
  fun supportedCases() =
    backendCases
      .filter { isSupported(it) }
      .toTypedArray()

  @DataProvider(name = "notSupportedBackendCases")
  fun notSupportedCases() =
    backendCases
      .filter { !isSupported(it) }
      .toTypedArray()

  abstract fun isApplicable(sourceFile: String): Boolean
  abstract fun isSupported(sourceFile: String): Boolean

  private fun doTest(caseName: String) {
    val newSourceFile =
      testDataDirectory.resolve("$caseName.fs").copyTo(activeSolutionDirectory.resolve("$caseName.fs.source"))

    changeFileContent(project, newSourceFile) {
      it.replace("{caret}", "<caret>")
        .replace("{selstart}", "<selstart>")
        .replace("{selend}", "<selend>")
    }

    try {
      withOpenedEditorFacade("Program.fs", "$caseName.fs.source") {
        typeOrCallAction(ideAction)
      }
      TestLoggerHelper.testErrorsAccumulator.throwIfNotEmpty()
    } catch (e: Throwable) {
      if (isSupported(caseName)) throw e
      else return
    }
    if (isSupported(caseName)) return
    else throw Exception(
      "Hooray! Expected to be not supported, but it was (frontend changes are equivalent to backend)! " +
        "Please add this case to supported ones."
    )
  }

  @Test(dataProvider = "supportedBackendCases")
  fun test(caseName: String) = doTest(caseName)

  @Test(dataProvider = "notSupportedBackendCases")
  fun notSupported(caseName: String) = doTest(caseName)
}


@Test
@Subsystem(SubsystemConstants.TYPING_ASSIST)
@Feature("Typing Assist")
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
class FSharpEnterTypingAssistSyncTest : FSharpBackendSyncTypingAssistTestBase(IdeActions.ACTION_EDITOR_ENTER) {
  val tests = setOf(
    "Enter - After comment - Indent 01",
    "Enter - After comment - Indent 02",
    "Enter - After comment - Indent 03",
    "Enter - After comment - Line start 05",
    "Enter - After comment - Line start 06",
    "Enter - After comment - Documentation 01",
    "Enter - After comment - Documentation 02",
    "Enter - After comment - Documentation 03 - Spaces",
    "Enter - Continue line - String 01",
    "Enter - Continue line - String 02",
    "Enter - Continue line - String 03",
    "Enter - Continue line - String 04",
    "Enter - Continue line - String 05",
    "Enter - Continue line - String 06",
    "Enter - Continue line - String 07",
    "Enter - Continue line - String 08",
    "Enter - Continue line - String 09",
    "Enter - Continue line - String 10",
    "Enter 00 - File beginning",
    "Enter 01 - No indent",
    "Enter 02 - Dumb indent",
    "Enter 03 - Dumb indent, trim spaces",
    "Enter 04 - Dumb indent, empty line",
    "Enter 11 - Left paren",
    "Enter 12 - Left paren and eol space",
    "Enter 13 - Left paren and space before",
    "Enter 14 - List, first element",
    "Enter 16 - After list",
    "Enter 17 - After multiple continued lines",
    "Enter 18 - After single continued line",
    "Enter 19 - After pair starting at line start",
    "Enter 27 - Empty line, dump indent",
    "Enter 29 - No indent before source",
    "Enter 30 - No indent before source 2",
    "Enter 32 - Nested binding",
    "Enter 34 - After line with multiple parens in row",
    "Enter 35 - Nested binding and indent",
    "Enter 45 - Before first list element in multiline list",
    "Enter 46 - Before first list element in multiline list",
    "Enter 50 - After line with attribute",
    "Enter 51 - Parens",
    "Enter 52 - Nested indents and parens",
    "Enter 56 - After then before source",
    "Enter 57 - After + before source",
    "Enter 58 - Object expression",
    "Enter 63 - In existing line",
    "Enter 65 - After larrow",
    "Enter 66 - After larrow - Comment",
    "Enter 68 - After larrow",
    "Enter 69 - After parens on new line",
    "Enter 70 - After parens on new line 2",
    "Enter 71 - After double semi",
    "Enter 72 - Semicolon inside attribute list",
    "Enter 75 - After equals",
    "Enter 77 - After equals",
    "Enter 78 - After equals",
    "Enter after error 05 - multiline if",
    "Enter in app 03",
    "Enter in app 04 - After last arg",
    "Enter in app 05 - After last arg and comment",
    "Enter in app 06 - Inside method invoke",
    "Enter in app 08 - Infix app",
    "Enter in app 09 - Multiline",
    "Enter in app 10 - Multiline",
    "Enter in app 11 - Multiline",
    "Enter in app 14 - After infix op",
    "Enter in comment 01",
    "Enter in comment 02",
    "Enter in comment 03",
    "Enter in comment 04 - Documentation 01",
    "Enter in string 01 - Inside empty triple-quoted string",
    "Enter in string 02 - Inside triple-quoted string",
    "Enter in string 03 - Inside triple-quoted string",
    "Enter in string 04 - Inside multiline triple-quoted string"
  )

  override fun isApplicable(testCase: String) =
    testCase.startsWith("Enter")

  override fun isSupported(sourceFile: String) =
    tests.contains(sourceFile)
}
