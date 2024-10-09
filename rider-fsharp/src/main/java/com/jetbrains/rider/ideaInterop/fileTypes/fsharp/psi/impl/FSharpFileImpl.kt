package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.extapi.psi.PsiFileBase
import com.intellij.psi.FileViewProvider
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpSignatureFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile

class FSharpFileImpl(viewProvider: FileViewProvider) : FSharpFile, PsiFileBase(viewProvider, FSharpLanguage) {
  override fun getFileType() = FSharpFileType
}

class FSharpSignatureFileImpl(viewProvider: FileViewProvider) : FSharpFile, PsiFileBase(viewProvider, FSharpLanguage) {
  override fun getFileType() = FSharpSignatureFileType
}

class FSharpScriptFileImpl(viewProvider: FileViewProvider) : FSharpFile, PsiFileBase(viewProvider, FSharpLanguage) {
  override fun getFileType() = FSharpScriptFileType
}
