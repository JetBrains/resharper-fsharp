package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpLine
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpReparseableElementType

class FSharpLineImpl(blockType: FSharpReparseableElementType, buffer: CharSequence?) : FSharpReparseableElementBase(blockType, buffer), FSharpLine{
    companion object {
        fun isParseable(chars : CharSequence): Boolean {
            return !(chars.contains('\n'))
        }
    }
}