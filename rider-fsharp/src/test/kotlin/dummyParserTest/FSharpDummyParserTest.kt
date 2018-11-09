package dummyParserTest

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpParserDefinition
import com.jetbrains.rider.test.RiderFrontendParserTest

class FSharpDummyParserTest : RiderFrontendParserTest("", "fs", FSharpParserDefinition()) {
    fun testSmoke01() = doTest()
    fun testBlocks01() = doTest()
    fun testLong01() = doTest()
}