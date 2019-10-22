import com.jetbrains.rider.model.RdFSharpCompilerServiceHost
import com.jetbrains.rider.model.rdFSharpModel
import com.jetbrains.rider.projectView.moveProviders.impl.ActionOrderType
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.framework.TestProjectModelContext
import com.jetbrains.rider.test.framework.waitBackend
import com.jetbrains.rider.test.scriptingApi.cutItem2
import com.jetbrains.rider.test.scriptingApi.pasteItem2
import com.jetbrains.rider.test.scriptingApi.renameItem2
import org.testng.annotations.Test

@Test
class FSharpProjectModelTest : ProjectModelBaseTest() {
    override fun getSolutionDirectoryName() = "EmptySolution"
    override val restoreNuGetPackages: Boolean
        get() = true

    private val fcsHost: RdFSharpCompilerServiceHost
        get() = project.solution.rdFSharpModel.fSharpCompilerServiceHost

    private fun moveItem(from: Array<Array<String>>, to: Array<String>, orderType: ActionOrderType? = null) {
        // Wait for updating/refreshing items possibly queued by FSharpItemsContainerRefresher.
        waitBackend(project) {
            cutItem2(project, from)
            pasteItem2(project, to, orderType = orderType)
        }
    }

    private fun moveItem(from: Array<String>, to: Array<String>, orderType: ActionOrderType? = null) {
        moveItem(arrayOf(from), to, orderType)
    }

    private fun renameItem(path: Array<String>, newName: String) {
        // Wait for updating/refreshing items possibly queued by FSharpItemsContainerRefresher.
        waitBackend(project) {
            renameItem2(project, path, newName)
        }
    }

    private fun TestProjectModelContext.dump2(caption: kotlin.String, checkSlnFile: kotlin.Boolean, compareProjFile: kotlin.Boolean, action: () -> kotlin.Unit) {
        dump(caption, checkSlnFile, compareProjFile, action)
        treeOutput.append(fcsHost.dumpSingleProjectMapping.sync(Unit))
    }

    @Test
    @TestEnvironment(solution = "FSharpProjectTree", toolset = ToolsetVersion.TOOLSET_16_CORE)
    fun testFSharpProjectStructure() {
        doTestDumpProjectsView {
            dump2("Init", false, false) {
            }
            dump2("1. Move file 'Folder(1)/File1.fs' inside other part of the same folder after 'Folder(2)/File4.fs'", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File1.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "File4.fs"))
            }
            dump2("2. Move file 'Folder(2)/File3.fs' inside other part of the same folder before 'Folder(1)/File2.fs'", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "File3.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File2.fs"), ActionOrderType.Before)
            }
            dump2("3. Move file 'Folder(2)/File1.fs' before folder 'Folder(2)'", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "File1.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2"), ActionOrderType.Before)
            }
            dump2("4. Move file 'File3.fs' and 'File1.fs' in folder 'Folder(2)/Sub(1)' before 'Class1.fs'", false, true) {
                moveItem(
                        arrayOf(
                                arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"),
                                arrayOf("FSharpProjectTree", "ClassLibrary1", "File1.fs")),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "Class1.fs"), ActionOrderType.Before)
            }
            dump2("5. Move 'Folder/Sub/File3.fs' to project folder before EmptyFolder", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "File3.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder"), ActionOrderType.Before)
            }
            dump2("6. Move 'Folder/Sub/File3.fs' to project folder after EmptyFolder", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder"), ActionOrderType.After)
            }
            dump2("7. Move file 'Class2.fs' in folder 'Folder(2)' before 'Sub(2)'", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?2", "Class2.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?2"), ActionOrderType.Before)
            }
            dump2("8. Move file 'Folder(1)/File2.fs' before folder 'Folder(1)/File3.fs'", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File2.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File3.fs"), ActionOrderType.Before)
            }
            dump2("9. Move file 'Folder/File2.fs' before 'Folder(1)'", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File2.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1"), ActionOrderType.Before)
            }
            dump2("10. Rename file 'File3.fs' to 'Foo.fs'", false, true) {
                renameItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"), "Foo.fs")
            }
            dump2("11. Move file 'Foo.fs' to 'EmptyFolder(1)'", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Foo.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder?1"))
            }
            dump2("12. Move file 'EmptyFolder/Foo.fs' before 'EmptyFolder(1)'", false, true) {
                moveItem(
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder?1", "Foo.fs"),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder?1"), ActionOrderType.Before)
            }
            dump2("Move file 'File1.fs' and 'Class1.fs' in folder 'Folder(2)' before 'Sub(1)'", false, true) {
                moveItem(
                        arrayOf(
                                arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "File1.fs"),
                                arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "Class1.fs")),
                        arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1"), ActionOrderType.Before)
            }
        }
    }
}