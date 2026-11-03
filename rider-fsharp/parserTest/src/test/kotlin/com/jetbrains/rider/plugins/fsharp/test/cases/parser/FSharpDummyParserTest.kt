package com.jetbrains.rider.plugins.fsharp.test.cases.parser

import com.intellij.platform.testFramework.junit5.codeInsight.fixture.ParsingTestFixture
import com.intellij.psi.PsiFile
import com.intellij.testFramework.LightVirtualFile
import com.intellij.testFramework.junit5.SystemPropertyClassLevel
import com.intellij.testFramework.junit5.TestApplication
import com.intellij.testFramework.junit5.fixture.TestFixtures
import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpParserDefinition
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpFileImpl
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpScriptFileImpl
import com.jetbrains.rider.test.base.psi.parsing.riderFrontendParserFixture
import com.jetbrains.rider.test.base.psi.parsing.riderFrontendParserProjectFixture
import org.junit.jupiter.api.Assertions
import org.junit.jupiter.api.Test

abstract class FSharpFrontendParserTest(private val fileType: RiderLanguageFileTypeBase) {
  protected abstract val parser: ParsingTestFixture

  protected open fun assertFileImpl(file: PsiFile) {}

  protected fun doTest() {
    val name = parser.testName
    val text = parser.loadFile("$name.${parser.fileExt}")
    // Create the file with the explicit [fileType]: the fixture otherwise derives it from the language, which is the
    // same for .fs and .fsx, so FSharpScriptFileType (and FSharpScriptFileImpl) would never be chosen.
    val virtualFile = LightVirtualFile("$name.${parser.fileExt}", fileType, text).apply { charset = Charsets.UTF_8 }
    val file = parser.checkParsedFile(parser.createFile(virtualFile), text)
    assertFileImpl(file)
    parser.checkResult(name, file)
  }
}


@SystemPropertyClassLevel("rider.disable.resharper.backend", "true")
@TestApplication
@TestFixtures
class FSharpDummyParserTest : FSharpFrontendParserTest(FSharpFileType) {
  companion object {
    private val projectFixture = riderFrontendParserProjectFixture()
  }

  override val parser: ParsingTestFixture by riderFrontendParserFixture(projectFixture, FSharpFileType.defaultExtension, FSharpParserDefinition())

  override fun assertFileImpl(file: PsiFile) = Assertions.assertTrue(file is FSharpFileImpl)

  @Test fun `test empty`() = doTest()
  @Test fun `test concatenation 01 - simple`() = doTest()
  @Test fun `test concatenation 02 - space before plus`() = doTest()
  @Test fun `test concatenation 03 - multiline`() = doTest()
  //TODO: compromise to avoid proper parsing
  @Test fun `test concatenation 04 - multiline with wrong offset 01`() = doTest()
  //TODO: compromise to avoid proper parsing
  @Test fun `test concatenation 04 - multiline with wrong offset 02`() = doTest()
  @Test fun `test concatenation 05 - with ident`() = doTest()
  @Test fun `test concatenation 06 - unfinished`() = doTest()
  @Test fun `test concatenation 07 - multiline string`() = doTest()
  //TODO: compromise to avoid proper parsing
  @Test fun `test concatenation 08 - multiline string with wrong offset`() = doTest()
  @Test fun `test concatenation 09 - with interpolated`() = doTest()
  @Test fun `test concatenation 10 - with expression`() = doTest()

  @Test fun `test regular strings 01`() = doTest()
  @Test fun `test regular strings 02 - unfinished`() = doTest()

  @Test fun `test interpolated strings 01`() = doTest()
  @Test fun `test interpolated strings 02 - unfinished`() = doTest()

  @Test fun `test unfinished 01 - regular`() = doTest()
  @Test fun `test unfinished 02 - interpolated 01`() = doTest()
  @Test fun `test unfinished 02 - interpolated 02`() = doTest()
  @Test fun `test unfinished 03 - interpolated in interpolated`() = doTest()
}


@SystemPropertyClassLevel("rider.disable.resharper.backend", "true")
@TestApplication
@TestFixtures
class FSharpScriptDummyParserTest : FSharpFrontendParserTest(FSharpScriptFileType) {
  companion object {
    private val projectFixture = riderFrontendParserProjectFixture()
  }

  override val parser: ParsingTestFixture by riderFrontendParserFixture(projectFixture, FSharpScriptFileType.defaultExtension, FSharpParserDefinition())

  override fun assertFileImpl(file: PsiFile) = Assertions.assertTrue(file is FSharpScriptFileImpl)

  @Test fun `test empty`() = doTest()
}
