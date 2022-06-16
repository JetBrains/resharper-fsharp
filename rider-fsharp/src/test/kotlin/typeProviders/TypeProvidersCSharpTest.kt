package typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rdclient.testFramework.waitForNextDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
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
            typeFromOffset("1", 195)
            waitForDaemon()

            executeWithGold(File(testGoldFile.path + "_after")) {
                dumpSevereHighlighters(it)
            }
        }
    }

    @Test
    fun `provided abbreviation rename`() {
        withOpenedEditor("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.cs") {
            waitForDaemon()
            defaultRefactoringRename("Renamed")
            waitForNextDaemon()
            markupAdapter.hasErrors.shouldBeFalse()
            executeWithGold(File(testGoldFile.path + " - csharp")) {
                dumpOpenedDocument(it, project!!)
            }
        }

        withOpenedEditor("YamlProviderLibrary/Library.fs") {
            waitForDaemon()
            markupAdapter.hasErrors.shouldBeFalse()
            executeWithGold(File(testGoldFile.path + " - fsharp")) {
                dumpOpenedDocument(it, project!!)
            }
        }
    }

    @Test
    @TestEnvironment(solution = "YamlProviderCSharp", toolset = ToolsetVersion.TOOLSET_16)
    fun `provided type abbreviation completion`() = doTestDumpLookupItems("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.fs")

    @Test
    @TestEnvironment(solution = "YamlProviderCSharp", toolset = ToolsetVersion.TOOLSET_16)
    fun `provided nested type completion`() = doTestDumpLookupItems("CSharpLibrary/CSharpLibrary.cs", "CSharpLibrary.fs")

    private fun doTestDumpLookupItems(relativePath: String, sourceFileName: String) {
        withOpenedEditor(relativePath, sourceFileName) {
            waitForDaemon()
            callBasicCompletion()
            waitForCompletion()
            executeWithGold(testGoldFile) {
                dumpActiveLookupItemsPresentations(it)
            }
        }
    }
}
