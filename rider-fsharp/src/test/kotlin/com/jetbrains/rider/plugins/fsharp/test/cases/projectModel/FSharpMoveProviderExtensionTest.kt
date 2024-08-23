package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.jetbrains.rd.ide.model.RdDndOrderType
import com.jetbrains.rider.plugins.fsharp.projectView.FSharpMoveProviderExtension
import com.jetbrains.rider.projectView.ProjectEntityView
import com.jetbrains.rider.projectView.solutionName
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.getProjectModelEntity
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.TestProjectModelDumpFilesProfile
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.maskAllAccordingDumpFilesProfile
import com.jetbrains.rider.test.scriptingApi.createDataContextFor
import com.jetbrains.rider.test.scriptingApi.dumpSolutionExplorerTree
import com.jetbrains.rider.test.scriptingApi.prepareProjectView
import org.testng.Assert
import org.testng.annotations.Test

@Test
class FSharpMoveProviderExtensionTest : ProjectModelBaseTest() {
  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_CORE_3_1)
  @Solution("MoveProviderSolution1")
  fun testAllowPaste01_Mix() {
    doTest { provider ->
      val compileBeforeFile = findFile("Project", "CompileBeforeFile.fs")
      val compileFile = findFile("Project", "CompileFile.fs")
      val targetFile = findFileView("Project", "TargetFile.fs")

      Assert.assertTrue(
        provider.allowPaste(listOf(compileFile, compileBeforeFile), targetFile, RdDndOrderType.None)
      )
      Assert.assertFalse(
        provider.allowPaste(listOf(compileFile, compileBeforeFile), targetFile, RdDndOrderType.Before)
      )
      Assert.assertFalse(
        provider.allowPaste(listOf(compileFile, compileBeforeFile), targetFile, RdDndOrderType.After)
      )
    }
  }

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_CORE_3_1)
  @Solution("MoveProviderSolution2")
  fun testAllowPaste02_DifferentFiles() {
    doTest { provider ->
      val files = arrayListOf(
        findFileView("TargetProject", "File1.fs"),
        findFileView("TargetProject", "File2.fs"),
        findFileView("TargetProject", "File3.fs"),
        findFileView("TargetProject", "File4.fs"),
        findFileView("TargetProject", "File5.fs"),
        findFileView("TargetProject", "File6.fs")
      )

      // Compile case
      val compileFile = listOf(findFile("SourceProject", "CompileFile.fs"))
      Assert.assertFalse(provider.allowPaste(compileFile, files[0], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileFile, files[0], RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileFile, files[1], RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileFile, files[1], RdDndOrderType.After))
      Assert.assertTrue(provider.allowPaste(compileFile, files[2], RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileFile, files[2], RdDndOrderType.After))
      Assert.assertTrue(provider.allowPaste(compileFile, files[3], RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileFile, files[3], RdDndOrderType.After))
      Assert.assertTrue(provider.allowPaste(compileFile, files[4], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileFile, files[4], RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileFile, files[5], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileFile, files[5], RdDndOrderType.After))

      // CompileBefore case
      val compileBeforeFile = listOf(findFile("SourceProject", "CompileBeforeFile.fs"))
      Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[0], RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[0], RdDndOrderType.After))
      Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[1], RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[1], RdDndOrderType.After))
      Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[2], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[2], RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[3], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[3], RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[4], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[4], RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[5], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[5], RdDndOrderType.After))

      // CompileAfter
      val compileAfterFile = listOf(findFile("SourceProject", "CompileAfterFile.fs"))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, files[0], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, files[0], RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, files[1], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, files[1], RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, files[2], RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, files[2], RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, files[3], RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileAfterFile, files[3], RdDndOrderType.After))
      Assert.assertTrue(provider.allowPaste(compileAfterFile, files[4], RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileAfterFile, files[4], RdDndOrderType.After))
      Assert.assertTrue(provider.allowPaste(compileAfterFile, files[5], RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileAfterFile, files[5], RdDndOrderType.After))
    }
  }

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_CORE_3_1)
  @Solution("MoveProviderSolution3")
  fun testAllowPaste03_DifferentFilesInFolders() {
    doTest { provider ->
      val rootFile = findFileView("TargetProject", "File3.fs")
      val folder1 = findFileView("TargetProject", "Folder1")
      val folder2 = findFileView("TargetProject", "Folder2")

      // Compile case
      val compileFile = listOf(findFile("SourceProject", "CompileFile.fs"))
      Assert.assertFalse(provider.allowPaste(compileFile, folder1, RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileFile, folder1, RdDndOrderType.After))
      Assert.assertTrue(provider.allowPaste(compileFile, rootFile, RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileFile, rootFile, RdDndOrderType.After))
      Assert.assertTrue(provider.allowPaste(compileFile, folder2, RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileFile, folder2, RdDndOrderType.After))

      // CompileBefore case
      val compileBeforeFile = listOf(findFile("SourceProject", "CompileBeforeFile.fs"))
      Assert.assertTrue(provider.allowPaste(compileBeforeFile, folder1, RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, folder1, RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, rootFile, RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, rootFile, RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, folder2, RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileBeforeFile, folder2, RdDndOrderType.After))

      // CompileAfter
      val compileAfterFile = listOf(findFile("SourceProject", "CompileAfterFile.fs"))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, folder1, RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, folder1, RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, rootFile, RdDndOrderType.Before))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, rootFile, RdDndOrderType.After))
      Assert.assertFalse(provider.allowPaste(compileAfterFile, folder2, RdDndOrderType.Before))
      Assert.assertTrue(provider.allowPaste(compileAfterFile, folder2, RdDndOrderType.After))
    }
  }

  private fun doTest(action: (FSharpMoveProviderExtension) -> Unit) {
    prepareProjectView(project)
    executeWithGold(testGoldFile) {
      it.append(dumpSolutionExplorerTree(project).maskAllAccordingDumpFilesProfile(TestProjectModelDumpFilesProfile()))
    }
    action(FSharpMoveProviderExtension(project))
  }

  private fun findFile(vararg localPath: String): ProjectModelEntity {
    val path = arrayOf(project.solutionName, *localPath)
    return createDataContextFor(project, path)
      .getProjectModelEntity(false)
      .shouldNotBeNull("Can not find item '${path.joinToString("/")}'")
  }

  private fun findFileView(vararg path: String): ProjectEntityView {
    return ProjectEntityView(project, findFile(*path))
  }
}
