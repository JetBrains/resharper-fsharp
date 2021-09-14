package typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test
import withOutOfProcessTypeProviders

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class TypeProvidersTest : BaseTypeProvidersTest() {
    override fun getSolutionDirectoryName() = "TypeProviderLibrary"

    @Test
    fun swaggerProvider() = doTest("SwaggerProvider")

    @Test
    fun simpleErasedProvider() = doTest("SimpleErasedProvider")

    @Test
    fun simpleGenerativeProvider() = doTest("SimpleGenerativeProvider")

    @Test
    fun providersErrors() = doTest("ProvidersErrors")

    @Test(description = "RIDER-60909")
    @TestEnvironment(solution = "LegacyTypeProviderLibrary")
    fun legacyTypeProviders() = doTest("LegacyTypeProviders")

    private fun doTest(fileName: String) {
        withOutOfProcessTypeProviders {
            withOpenedEditor(project, "TypeProviderLibrary/$fileName.fs") {
                waitForDaemon()
                executeWithGold(testGoldFile) {
                    dumpSevereHighlighters(it)
                }
            }
        }
    }
}
