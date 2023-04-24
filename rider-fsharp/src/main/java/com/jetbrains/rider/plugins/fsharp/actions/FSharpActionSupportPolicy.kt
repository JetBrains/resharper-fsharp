package com.jetbrains.rider.plugins.fsharp.actions

import com.intellij.psi.PsiElement
import com.jetbrains.rider.actions.RiderActionCallStrategy
import com.jetbrains.rider.actions.RiderActionSupportPolicy
import com.jetbrains.rider.actions.RiderActions

class FSharpActionSupportPolicy : RiderActionSupportPolicy() {
  override fun getCallStrategy(psiElement: PsiElement, backendActionId: String): RiderActionCallStrategy {
    return if (backendActionId == RiderActions.GOTO_DECLARATION) RiderActionCallStrategy.BACKEND_FIRST
    else super.getCallStrategy(psiElement, backendActionId)
  }
}
