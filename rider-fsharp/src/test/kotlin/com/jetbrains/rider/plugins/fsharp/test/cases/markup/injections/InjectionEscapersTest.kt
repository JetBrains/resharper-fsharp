package com.jetbrains.rider.plugins.fsharp.test.cases.markup.injections

import com.intellij.codeInsight.daemon.impl.HighlightInfoType
import com.intellij.codeInsight.intention.impl.QuickEditAction
import com.intellij.lang.injection.InjectedLanguageManager
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.fileEditor.ex.FileEditorManagerEx
import com.intellij.psi.impl.source.tree.injected.InjectedLanguageUtil
import com.jetbrains.rider.editors.getPsiFile
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.getHighlighters
import com.jetbrains.rider.test.scriptingApi.typeFromOffset
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.test.waitForDaemon
import org.testng.annotations.Test

@Test
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class InjectionEscapersTest : BaseTestWithSolution() {
  override fun getSolutionDirectoryName() = "CoreConsoleApp"

  private fun doTest(action: (EditorImpl, EditorImpl) -> Unit) {
    withOpenedEditor("Program.fs", "Program.fs") {
      waitForDaemon()
      val hostFile = getPsiFile()!!
      val project = project!!
      val offset = this.caretModel.offset
      val injectedLanguageManager = InjectedLanguageManager.getInstance(project)
      val fileEditorManager = FileEditorManagerEx.getInstanceEx(project)
      val element = injectedLanguageManager.findInjectedElementAt(hostFile, offset)
      val quickEditHandler = QuickEditAction().invokeImpl(project, this, hostFile)
      val documentWindow = InjectedLanguageUtil.getDocumentWindow(element!!.containingFile)
      val unescapedOffset = InjectedLanguageUtil.hostToInjectedUnescaped(documentWindow, offset)
      val fragmentEditor =
        fileEditorManager.openTextEditor(
          OpenFileDescriptor(
            project,
            quickEditHandler.newFile.virtualFile,
            unescapedOffset
          ), true
        ) as EditorImpl

      try {
        action(this, fragmentEditor)
      } finally {
        quickEditHandler.closeEditorForTest()
      }
    }
  }

  private fun doBackslashTest() = doTest { hostEditor, fragmentEditor ->
    fragmentEditor.typeFromOffset("\\", fragmentEditor.caretModel.offset)
    executeWithGold(testGoldFile) { printStream ->
      printStream.print(
        getHighlighters(project, hostEditor) {
          it.severity === HighlightInfoType.INJECTED_FRAGMENT_SEVERITY
        }
      )
    }
  }

  private fun doFullEditingTest() = doTest { hostEditor, fragmentEditor ->
    executeWithGold(testGoldFile) { printStream ->
      printStream.println("---Fragment editor---")
      printStream.println(getHighlighters(project, fragmentEditor))

      fragmentEditor.typeFromOffset(" ", fragmentEditor.caretModel.offset)

      printStream.println("\n---Host editor after editing---")
      printStream.print(
        getHighlighters(project, hostEditor) {
          it.severity === HighlightInfoType.INJECTED_FRAGMENT_SEVERITY
        }
      )
    }
  }

  fun `escaping - regular`() = doFullEditingTest()
  @Mute("RIDER-111883", platforms = [PlatformType.LINUX_ALL, PlatformType.MAC_OS_ALL])
  fun `escaping - regular - interpolated`() = doFullEditingTest()
  fun `escaping - triple quoted`() = doFullEditingTest()
  fun `escaping - triple quoted - interpolated`() = doFullEditingTest()
  fun `escaping - verbatim`() = doFullEditingTest()
  fun `escaping - verbatim - interpolated`() = doFullEditingTest()
  fun `escaping - raw`() = doFullEditingTest()

  fun `backslash - simple`() = doBackslashTest()
  fun `backslash at the end - regular`() = doBackslashTest()
  fun `backslash at the end - verbatim`() = doBackslashTest()
  fun `backslash at the end - triple quoted`() = doBackslashTest()
  fun `backslash at the end - raw`() = doBackslashTest()
  fun `backslash before hole - interpolated`() = doBackslashTest()
  fun `escaped backslash at the end - regular`() = doBackslashTest()
}
