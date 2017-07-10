package com.jetbrains.rider.plugins.fsharp.actions

import com.intellij.psi.PsiFile
import com.jetbrains.rider.actions.RiderActionCallStrategy
import com.jetbrains.rider.actions.RiderActionSupportPolicy

class FSharpActionSupportPolicy : RiderActionSupportPolicy() {
    override fun getCallStrategy(psiFile: PsiFile, offset: Int?, reSharperId: String): RiderActionCallStrategy {
        if (reSharperId == "TextControl.Paste")
            return RiderActionCallStrategy.FRONTEND_ONLY
        return super.getCallStrategy(psiFile, offset, reSharperId)
    }
}
