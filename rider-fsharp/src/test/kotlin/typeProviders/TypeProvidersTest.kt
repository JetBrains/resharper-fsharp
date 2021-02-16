package typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test
import withTypeProviders

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class TypeProvidersTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "TypeProviderLibrary"
    override val restoreNuGetPackages = true

    @Test
    fun swaggerProvider() = doTest("SwaggerProvider")

    @Test
    fun simpleErasedProvider() = doTest("SimpleErasedProvider")

    @Test
    fun simpleGenerativeProvider() = doTest("SimpleGenerativeProvider")

    @Test
    fun providersErrors() = doTest("ProvidersErrors")

    private fun doTest(fileName: String) {
        withTypeProviders {
            withOpenedEditor(project, "TypeProviderLibrary/$fileName.fs") {
                waitForDaemon()
                executeWithGold(testGoldFile) {
                    dumpSevereHighlighters(it)
                }
            }
        }
    }
}
