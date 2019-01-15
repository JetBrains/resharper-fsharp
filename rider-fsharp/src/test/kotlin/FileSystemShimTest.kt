import com.intellij.openapi.vfs.LocalFileSystem
import com.jetbrains.rider.model.RdFSharpCompilerServiceHost
import com.jetbrains.rider.model.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.scriptingApi.changeFileContent
import com.jetbrains.rider.util.idea.lifetime
import com.jetbrains.rider.util.idea.waitAndPump
import com.jetbrains.rd.util.reactive.adviseOnce
import org.testng.annotations.Test
import java.io.File

@Test
class FileSystemShimTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "CoreConsoleApp"

    private val fcsHost: RdFSharpCompilerServiceHost
        get() = project.solution.rdFSharpModel.fSharpCompilerServiceHost

    @Test
    fun externalFileChange() {
        val fcsHost = fcsHost
        val file = activeSolutionDirectory.resolve("Program.fs")
        val stampBefore = getTimestamp(file)

        val newText = "namespace NewTextHere"
        changeFileContent(project, file) { newText }

        LocalFileSystem.getInstance().refresh(false)
        waitAndPump(project.lifetime, { getTimestamp(file) > stampBefore }, 15000, { "Timestamp wasn't changed." })
        val stampAfter = getTimestamp(file)

        val (source, timestamp) = fcsHost.getSourceCache.sync(file.path).shouldNotBeNull("Couldn't get the source.")
        assert(source == newText) { "Source differs from new text." }
        assert(timestamp == stampAfter) { "Timestamp differs from expected." }
    }

    private fun getTimestamp(file: File) =
            fcsHost.getLastModificationStamp.sync(file.path)

}
