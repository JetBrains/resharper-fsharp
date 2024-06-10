package com.jetbrains.rider.plugins.fsharp.llm

import com.intellij.ml.llm.privacy.PSString
import com.intellij.ml.llm.privacy.trustedStringBuilders.dangerousConvert
import com.intellij.ml.llm.smartChat.psiSummarization.LanguageSummaryProvider
import com.intellij.psi.PsiDocumentManager
import com.intellij.psi.PsiElement
import com.intellij.util.concurrency.ThreadingAssertions
import com.jetbrains.rd.ide.model.RdDocumentId
import com.jetbrains.rdclient.document.getDocumentId
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile
import com.jetbrains.rider.model.GetFileSummaryParameters
import com.jetbrains.rider.model.aIChatModel
import com.jetbrains.rider.projectView.solution
import kotlinx.coroutines.runBlocking

class FSharpFileSummarizer: LanguageSummaryProvider {
  override fun generateSummary(psiElement: PsiElement): PSString? {
    val psiFile = psiElement.containingFile
    if (psiFile is FSharpFile) {
      val ids: RdDocumentId = PsiDocumentManager.getInstance(psiFile.project).getDocument(psiFile)?.getDocumentId(psiFile.project)
                              ?: return null

      ThreadingAssertions.assertBackgroundThread()
      return runBlocking {
        return@runBlocking psiFile.project.solution.aIChatModel.getFileSummary.startSuspending(
          GetFileSummaryParameters(ids)).summary.dangerousConvert
      }
    }
    return null
  }
}

