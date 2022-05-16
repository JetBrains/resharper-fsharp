package typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16)
class TypeProvidersCSharpTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "YamlProviderCSharp"
    override val restoreNuGetPackages = true

    @Test
    fun resolveTest() {
        withOpenedEditor(project, "CSharpLibrary/CSharpLibrary.cs") {
            waitForDaemon()
            executeWithGold(testGoldFile) {
                dumpSevereHighlighters(it)
            }
        }

        unloadAllProjects()
        reloadAllProjects(project)

        withOpenedEditor(project, "CSharpLibrary/CSharpLibrary.cs") {
            waitForDaemon()
            executeWithGold(testGoldFile) {
                dumpSevereHighlighters(it)
            }
        }
    }

    @Test
    @TestEnvironment(
        solution = "SwaggerProviderCSharp",
        toolset = ToolsetVersion.TOOLSET_17_CORE,
        coreVersion = CoreVersion.DOT_NET_6
    )
    fun changeStaticArg() {
        withOpenedEditor(project, "SwaggerProviderLibrary/Literals.fs") {
            // change schema path from "specification.json" to "specification1.json"
            typeFromOffset("1", 86)
        }

        withOpenedEditor(project, "CSharpLibrary/CSharpLibrary.cs") {
            waitForDaemon()
            executeWithGold(File(testGoldFile.path + "_before")) {
                dumpSevereHighlighters(it)
            }

            // change method call from "ApiCoursesGet" to "ApiCoursesGet1"
            typeFromOffset("1", 194)
            waitForDaemon()

            executeWithGold(File(testGoldFile.path + "_after")) {
                dumpSevereHighlighters(it)
            }
        }
    }
}
