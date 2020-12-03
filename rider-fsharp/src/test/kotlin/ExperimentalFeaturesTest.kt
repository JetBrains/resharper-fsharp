import com.jetbrains.rider.plugins.fsharp.RdFSharpFeatures
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import extensions.withExperimentalFeatures
import org.testng.annotations.Test

@Test
class ExperimentalFeaturesTest : ProjectModelBaseTest() {
    override fun getSolutionDirectoryName() = "EmptySolution"

    private val fcsHost get() = project.solution.rdFSharpModel.fsharpTestHost

    @Test
    fun isTestEnvironment() {
        fcsHost.isTestEnvironment.sync(Unit).shouldBeFalse()

        withExperimentalFeatures(project, arrayOf(RdFSharpFeatures.TestEnvironment)) {
            fcsHost.isTestEnvironment.sync(Unit).shouldBeTrue()
        }

        fcsHost.isTestEnvironment.sync(Unit).shouldBeFalse()
    }
}
