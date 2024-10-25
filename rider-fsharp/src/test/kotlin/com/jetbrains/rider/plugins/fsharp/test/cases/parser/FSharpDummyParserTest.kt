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
import com.jetbrains.rider.test.cases.psi.parsing.RiderFrontendParserTest
import org.junit.Assert

abstract class FSharpFrontendParserTest(private val fileType: RiderLanguageFileTypeBase) :
  RiderFrontendParserTest("", fileType.defaultExtension, FSharpParserDefinition()) {

  protected open fun assertFileImpl(file: PsiFile) {}

  override fun parseFile(name: String?, text: String?): PsiFile {
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

  fun `test empty`() = doTest()
  fun `test concatenation 01 - simple`() = doTest()
  fun `test concatenation 02 - space before plus`() = doTest()
  fun `test concatenation 03 - multiline`() = doTest()
  //TODO: compromise to avoid proper parsing
  fun `test concatenation 04 - multiline with wrong offset 01`() = doTest()
  //TODO: compromise to avoid proper parsing
  fun `test concatenation 04 - multiline with wrong offset 02`() = doTest()
  fun `test concatenation 05 - with ident`() = doTest()
  fun `test concatenation 06 - unfinished`() = doTest()
  fun `test concatenation 07 - multiline string`() = doTest()
  //TODO: compromise to avoid proper parsing
  fun `test concatenation 08 - multiline string with wrong offset`() = doTest()
  fun `test concatenation 09 - with interpolated`() = doTest()
  fun `test concatenation 10 - with expression`() = doTest()

  fun `test regular strings 01`() = doTest()
  fun `test regular strings 02 - unfinished`() = doTest()

  fun `test interpolated strings 01`() = doTest()
  fun `test interpolated strings 02 - unfinished`() = doTest()

  fun `test unfinished 01 - regular`() = doTest()
  fun `test unfinished 02 - interpolated 01`() = doTest()
  fun `test unfinished 02 - interpolated 02`() = doTest()
  fun `test unfinished 03 - interpolated in interpolated`() = doTest()
}


class FSharpScriptDummyParserTests : FSharpFrontendParserTest(FSharpScriptFileType) {
  override fun assertFileImpl(file: PsiFile) = Assert.assertTrue(file is FSharpScriptFileImpl)

  fun `test empty`() = doTest()
}
