package dummyParserTest

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpParserDefinition
import com.jetbrains.rider.test.RiderFrontendParserTest

class FSharpDummyParserTest : RiderFrontendParserTest("", "fs", FSharpParserDefinition()) {
    fun testSmoke01() = doTest()
    fun testBlocks01() = doTest()
    fun testLong01() = doTest()
    fun testMultiline01() = doTest()
    fun testMultiline02() = doTest()
    fun testMultiline03() = doTest()
    fun testMultiline04() = doTest()
}