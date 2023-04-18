package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.extapi.psi.PsiFileBase
import com.intellij.psi.FileViewProvider
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile

class FSharpFileImpl(viewProvider: FileViewProvider) : FSharpFile, PsiFileBase(viewProvider, FSharpLanguage) {
  override fun getFileType() = FSharpFileType
}
