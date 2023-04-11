package com.jetbrains.rider.plugins.fsharp.test.cases.parser

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpParserDefinition
import com.jetbrains.rider.test.RiderFrontendParserTest

class FSharpDummyParserTests : RiderFrontendParserTest("", "fs", FSharpParserDefinition()) {
  fun `test concatenation 01 - simple`() = doTest()
  fun `test concatenation 02 - space before plus`() = doTest()
  fun `test concatenation 03 - multiline`() = doTest()
  //TODO: compromise to avoid fair parsing
  fun `test concatenation 04 - multiline with wrong offset 01`() = doTest()
  fun `test concatenation 04 - multiline with wrong offset 02`() = doTest()
  fun `test concatenation 05 - with ident`() = doTest()
  fun `test concatenation 06 - unfinished`() = doTest()
  fun `test concatenation 07 - multiline string`() = doTest()
  fun `test concatenation 08 - multiline string with wrong offset`() = doTest()
  fun `test concatenation 09 - with interpolated`() = doTest()

  fun `test regular strings 01`() = doTest()
  fun `test regular strings 02 - unfinished`() = doTest()

  fun `test interpolated strings 01`() = doTest()
  fun `test interpolated strings 02 - unfinished`() = doTest()

  fun `test unfinished 01 - regular`() = doTest()
  fun `test unfinished 02 - interpolated 01`() = doTest()
  fun `test unfinished 02 - interpolated 02`() = doTest()
}
