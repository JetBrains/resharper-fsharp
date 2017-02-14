package com.jetbrains.resharper.editorActions

import com.intellij.psi.PsiFile
import com.jetbrains.resharper.ideaInterop.fileTypes.fsharp.FSharpFileType

class FSharpTypeHandler : RiderTypeHandlerBase() {
    override fun isApplicable(file: PsiFile): Boolean {
        return file.fileType is FSharpFileType
    }
}