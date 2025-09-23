package com.jetbrains.rider.plugins.fsharp.test.cases.parser

import com.intellij.lang.ParserDefinition
import com.intellij.mock.MockFileTypeManager
import com.intellij.openapi.fileTypes.FileTypeManager
import com.intellij.psi.PsiFile
import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpParserDefinition
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpFileImpl
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpScriptFileImpl
import com.jetbrains.rider.test.base.psi.parsing.RiderFrontendParserTest
import org.junit.Assert
import org.junit.Test

abstract class FSharpFrontendParserTest(private val fileType: RiderLanguageFileTypeBase) :
  RiderFrontendParserTest("", fileType.defaultExtension, FSharpParserDefinition()) {

  protected open fun assertFileImpl(file: PsiFile) {}

  override fun parseFile(name: String, text: String): PsiFile {
    val file = super.parseFile(name, text)
    assertFileImpl(file)
    return file
  }

  override fun configureFromParserDefinition(definition: ParserDefinition, extension: String?) {
    super.configureFromParserDefinition(definition, extension)
    application.picoContainer.unregisterComponent(FileTypeManager::class.java.name)
    application.registerService(FileTypeManager::class.java, MockFileTypeManager(fileType), testRootDisposable)
  }
}


class FSharpDummyParserTests : FSharpFrontendParserTest(FSharpFileType) {
  override fun assertFileImpl(file: PsiFile) = Assert.assertTrue(file is FSharpFileImpl)

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


class FSharpScriptDummyParserTests : FSharpFrontendParserTest(FSharpScriptFileType) {
  override fun assertFileImpl(file: PsiFile) = Assert.assertTrue(file is FSharpScriptFileImpl)

  @Test fun `test empty`() = doTest()
}
