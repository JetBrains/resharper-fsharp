package com.jetbrains.rider.plugins.fsharp.test.cases.parser

import com.intellij.psi.PsiFile
import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpParserDefinition
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpFileImpl
import com.jetbrains.rider.test.base.psi.parsing.RiderFrontendParserTest
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Tag(TeamCityTags.Plugins.FSharp.Unit)
class FSharpDummyParserTest : RiderFrontendParserTest("fs", FSharpParserDefinition()) {
  override fun assertFileImpl(file: PsiFile) = Assertions.assertTrue(file is FSharpFileImpl)

  override val fileType: RiderLanguageFileTypeBase = FSharpFileType

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
