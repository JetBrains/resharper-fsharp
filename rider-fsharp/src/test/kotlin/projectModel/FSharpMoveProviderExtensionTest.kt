import com.intellij.openapi.project.Project
import com.jetbrains.rider.model.RdNullLocation
import com.jetbrains.rider.model.RdProjectFileDescriptor
import com.jetbrains.rider.model.RdProjectFolderDescriptor
import com.jetbrains.rider.plugins.fsharp.projectView.FSharpMoveProviderExtension
import com.jetbrains.rider.projectView.moveProviders.impl.ActionOrderType
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.nodes.ProjectModelNodeKey
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import org.testng.Assert
import org.testng.annotations.Test

@Test
class FSharpMoveProviderExtensionTest : ProjectModelBaseTest() {
    override fun getSolutionDirectoryName() = "EmptySolution"

    @Test
    fun testAllowPaste01() {
        doTest { provider ->
            Assert.assertTrue(
                    provider.allowPaste(listOf(project.createFile()), project.createFile(), ActionOrderType.None)
            )
        }
    }

    @Test
    fun testAllowPaste02_Mix() {
        doTest { provider ->
            Assert.assertTrue(
                    provider.allowPaste(listOf(project.createFile(), project.createCompileBeforeFile()),
                            project.createFile(), ActionOrderType.None)
            )
            Assert.assertFalse(
                    provider.allowPaste(listOf(project.createFile(), project.createCompileBeforeFile()),
                            project.createFile(), ActionOrderType.Before)
            )
            Assert.assertFalse(
                    provider.allowPaste(listOf(project.createFile(), project.createCompileBeforeFile()),
                            project.createFile(), ActionOrderType.After)
            )
        }
    }

    @Test
    fun testAllowPaste03_DifferentFiles() {
        doTest { provider ->
            /* CompileBefore [0]
               CompileBefore [1]
               Compile       [2]
               Compile       [3]
               CompileAfter  [4]
               CompileAfter  [5]
            * */
            val root = project.createFolder()
            val files = arrayListOf(
                    project.createCompileBeforeFile(1, root),
                    project.createCompileBeforeFile(2, root),
                    project.createFile(3, root),
                    project.createFile(4, root),
                    project.createCompileAfterFile(5, root),
                    project.createCompileAfterFile(6, root)
            )

            // Compile case
            val compileFile = listOf(project.createFile())
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
            val compileBeforeFile = listOf(project.createCompileBeforeFile())
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
            val compileAfterFile = listOf(project.createCompileAfterFile())
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
    fun testAllowPaste04_DifferentFilesInFolders() {
        doTest { provider ->
            /* Folder1/CompileBefore [0]
               Folder1/Compile       [1]
               Compile               [2]
               Folder2/Compile       [3]
               Folder2/CompileAfter  [4]
            * */
            val root = project.createFolder()
            val folder1 = project.createFolder(1, root).apply {
                project.createCompileBeforeFile(1, this)
                project.createFile(1, this)
            }
            val rootFile = project.createFile(2, root)
            val folder2 = project.createFolder(3, root).apply {
                project.createFile(1, this)
                project.createCompileAfterFile(2, this)
            }

            // Compile case
            val compileFile = listOf(project.createFile())
            Assert.assertFalse(provider.allowPaste(compileFile, folder1, ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileFile, folder1, ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileFile, rootFile, ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileFile, rootFile, ActionOrderType.After))
            Assert.assertTrue(provider.allowPaste(compileFile, folder2, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileFile, folder2, ActionOrderType.After))

            // CompileBefore case
            val compileBeforeFile = listOf(project.createCompileBeforeFile())
            Assert.assertTrue(provider.allowPaste(compileBeforeFile, folder1, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, folder1, ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, rootFile, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, rootFile, ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, folder2, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileBeforeFile, folder2, ActionOrderType.After))

            // CompileAfter
            val compileAfterFile = listOf(project.createCompileAfterFile())
            Assert.assertFalse(provider.allowPaste(compileAfterFile, folder1, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, folder1, ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, rootFile, ActionOrderType.Before))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, rootFile, ActionOrderType.After))
            Assert.assertFalse(provider.allowPaste(compileAfterFile, folder2, ActionOrderType.Before))
            Assert.assertTrue(provider.allowPaste(compileAfterFile, folder2, ActionOrderType.After))
        }
    }

    private fun Project.createCompileBeforeFile(order: Int = 0, parent: ProjectModelNode? = null): ProjectModelNode {
        return createFile(order, parent, FSharpMoveProviderExtension.CompileBeforeType)
    }

    private fun Project.createCompileAfterFile(order: Int = 0, parent: ProjectModelNode? = null): ProjectModelNode {
        return createFile(order, parent, FSharpMoveProviderExtension.CompileAfterType)
    }

    private fun Project.createFile(order: Int = 0, parent: ProjectModelNode? = null, itemType: String? = null): ProjectModelNode {
        val userData = if (itemType != null) "${FSharpMoveProviderExtension.FSharpCompileType}=$itemType" else null
        val descriptor = RdProjectFileDescriptor(false, false, listOf(), order, userData, "File.fs", RdNullLocation())
        return ProjectModelNode(this, ProjectModelNodeKey(0), descriptor, parent)
    }

    private fun Project.createFolder(order: Int = 0, parent: ProjectModelNode? = null): ProjectModelNode {
        val descriptor = RdProjectFolderDescriptor(false, false, false, false, order, "Folder", RdNullLocation())
        return ProjectModelNode(this, ProjectModelNodeKey(0), descriptor, parent)
    }

    private fun doTest(action: (FSharpMoveProviderExtension) -> Unit) {
        action(FSharpMoveProviderExtension(project))
    }
}