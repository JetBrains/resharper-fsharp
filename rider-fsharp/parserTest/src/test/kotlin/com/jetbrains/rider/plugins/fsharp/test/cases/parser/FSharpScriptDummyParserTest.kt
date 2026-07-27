package com.jetbrains.rider.plugins.fsharp.test.cases.parser

import com.intellij.psi.PsiFile
import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpParserDefinition
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpScriptFileImpl
import com.jetbrains.rider.test.base.psi.parsing.RiderFrontendParserTest
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test


@Tag(TeamCityTags.Plugins.FSharp.Unit)
class FSharpScriptDummyParserTest : RiderFrontendParserTest("fsx", FSharpParserDefinition()) {
  override fun assertFileImpl(file: PsiFile) = Assertions.assertTrue(file is FSharpScriptFileImpl)

  override val fileType: RiderLanguageFileTypeBase = FSharpScriptFileType

  @Test fun `test empty`() = doTest()
}
