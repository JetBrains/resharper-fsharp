package projectModel

import com.jetbrains.rider.plugins.fsharp.projectView.FSharpMoveProviderExtension
import com.jetbrains.rider.projectView.ProjectEntityView
import com.jetbrains.rider.projectView.moveProviders.impl.ActionOrderType
import com.jetbrains.rider.projectView.solutionName
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.getProjectModelEntity
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.createDataContextFor
import com.jetbrains.rider.test.scriptingApi.dumpSolutionExplorerTree
import com.jetbrains.rider.test.scriptingApi.prepareProjectView
import org.testng.Assert
import org.testng.annotations.Test

@Test
class FSharpMoveProviderExtensionTest : ProjectModelBaseTest() {

    override fun getSolutionDirectoryName() = error("Specify solution per test")

    @Test
    @TestEnvironment(solution = "MoveProviderSolution1", toolset = ToolsetVersion.TOOLSET_16_CORE)
    fun testAllowPaste01_Mix() {
        doTest { provider ->
            val compileBeforeFile = findFile("Project", "CompileBeforeFile.fs")
            val compileFile = findFile("Project", "CompileFile.fs")
            val targetFile = findFileView("Project", "TargetFile.fs")

            Assert.assertTrue(
                    provider.allowPaste(listOf(compileFile, compileBeforeFile), targetFile, ActionOrderType.None)
            )
            Assert.assertFalse(
                    provider.allowPaste(listOf(compileFile, compileBeforeFile), targetFile, ActionOrderType.Before)
            )
            Assert.assertFalse(
                    provider.allowPaste(listOf(compileFile, compileBeforeFile), targetFile, ActionOrderType.After)
            )
        }
    }

    @Test
    @TestEnvironment(solution = "MoveProviderSolution2", toolset = ToolsetVersion.TOOLSET_16_CORE)
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
            Assert.assertFalse(provider.allowPaste(compileFile, files[0], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileFile, files[0], ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileFile, files[1], ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileFile, files[1], ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileFile, files[2], ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileFile, files[2], ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileFile, files[3], ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileFile, files[3], ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileFile, files[4], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileFile, files[4], ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileFile, files[5], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileFile, files[5], ActionOrderType.After))

            // CompileBefore case
            val compileBeforeFile = listOf(findFile("SourceProject", "CompileBeforeFile.fs"))
            Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[0], ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[0], ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[1], ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[1], ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileBeforeFile, files[2], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[2], ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[3], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[3], ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[4], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[4], ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[5], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, files[5], ActionOrderType.After))

            // CompileAfter
            val compileAfterFile = listOf(findFile("SourceProject", "CompileAfterFile.fs"))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, files[0], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, files[0], ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, files[1], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, files[1], ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, files[2], ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, files[2], ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, files[3], ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileAfterFile, files[3], ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileAfterFile, files[4], ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileAfterFile, files[4], ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileAfterFile, files[5], ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileAfterFile, files[5], ActionOrderType.After))
        }
    }

    @Test
    @TestEnvironment(solution = "MoveProviderSolution3", toolset = ToolsetVersion.TOOLSET_16_CORE)
    fun testAllowPaste03_DifferentFilesInFolders() {
        doTest { provider ->
            val rootFile = findFileView("TargetProject", "File3.fs")
            val folder1 = findFileView("TargetProject", "Folder1")
            val folder2 = findFileView("TargetProject", "Folder2")

            // Compile case
            val compileFile = listOf(findFile("SourceProject", "CompileFile.fs"))
            Assert.assertFalse(provider.allowPaste(compileFile, folder1, ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileFile, folder1, ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileFile, rootFile, ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileFile, rootFile, ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileFile, folder2, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileFile, folder2, ActionOrderType.After))

            // CompileBefore case
            val compileBeforeFile = listOf(findFile("SourceProject", "CompileBeforeFile.fs"))
            Assert.assertTrue(provider.allowPaste(compileBeforeFile, folder1, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, folder1, ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, rootFile, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, rootFile, ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, folder2, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, folder2, ActionOrderType.After))

            // CompileAfter
            val compileAfterFile = listOf(findFile("SourceProject", "CompileAfterFile.fs"))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, folder1, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, folder1, ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, rootFile, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, rootFile, ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, folder2, ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileAfterFile, folder2, ActionOrderType.After))
        }
    }

    private fun doTest(action: (FSharpMoveProviderExtension) -> Unit) {
        prepareProjectView(project)
        executeWithGold(testGoldFile) {
            it.append(dumpSolutionExplorerTree(project))
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