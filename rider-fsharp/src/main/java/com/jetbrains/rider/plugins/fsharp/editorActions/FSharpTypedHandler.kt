package com.jetbrains.rider.plugins.fsharp.editorActions

import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.client.ClientAppSession
import com.intellij.openapi.editor.Caret
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.actionSystem.EditorActionHandler
import com.intellij.psi.util.PsiUtilBase
import com.jetbrains.rd.ide.model.TextControlId
import com.jetbrains.rdclient.editorActions.cwm.FrontendAsyncEditorActionHandler
import com.jetbrains.rdclient.editorActions.cwm.FrontendCallEditorActionRequest
import com.jetbrains.rdclient.editorActions.cwm.FrontendOnlyCallEditorActionRequest
import com.jetbrains.rdclient.requests.FrontendAsyncRequest
import com.jetbrains.rdclient.requests.createResetUndoHistoryToken
import com.jetbrains.rider.editorActions.FrontendTypedHandler
import com.jetbrains.rider.editorActions.RiderCallEditorActionRequestFactory
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguageBase

class FSharpTypedHandler : FrontendTypedHandler()


class FSharpCallEditorActionRequestFactory(private val session: ClientAppSession) : FrontendAsyncEditorActionHandler.CallEditorActionRequestFactory {
  val fallbackImplementation: RiderCallEditorActionRequestFactory = RiderCallEditorActionRequestFactory(session)
  override suspend fun createRequest(editor: Editor, editorId: TextControlId, frontendActionId: String, caret: Caret?, baseHandler: EditorActionHandler, dataContext: DataContext): FrontendAsyncRequest? {
    val language = editor.project?.run { PsiUtilBase.getLanguageInEditor(editor, this) } as? FSharpLanguageBase
    if (language != null) {
      val resetUndoHistoryToken = createResetUndoHistoryToken(editor)
      val request = fallbackImplementation.createRequest(editor, editorId, frontendActionId, caret, baseHandler, dataContext) as? FrontendCallEditorActionRequest

      if (request != null) {
        val patch = request.patch!!
        return FrontendOnlyCallEditorActionRequest(patch, editorId, frontendActionId, session, resetUndoHistoryToken, request.info)
      }
      else {
        return null
      }
    }
    return fallbackImplementation.createRequest(editor, editorId, frontendActionId, caret, baseHandler, dataContext)
  }
}
