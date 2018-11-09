package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpIndentationBlock
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpReparseableElementType

class FSharpIndentationBlockImpl(blockType: FSharpReparseableElementType, buffer: CharSequence?) : FSharpReparseableElementBase(blockType,buffer), FSharpIndentationBlock {
    companion object {
        fun isParseable(chars : CharSequence): Boolean {
            val indentOfBlock = chars.indexOfFirst { it != ' ' }

            for (i in chars.split("\n").filter { !it.isEmpty() }) {
                var indentOfCurrentLine = chars.indexOfFirst { it != ' ' }
                if (indentOfCurrentLine == -1) indentOfCurrentLine = chars.length

                if (indentOfCurrentLine < indentOfBlock) return false
            }
            return true
        }
    }
}
