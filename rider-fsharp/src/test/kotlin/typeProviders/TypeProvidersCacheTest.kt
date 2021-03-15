package typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.reloadAllProjects
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.unloadAllProjects
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import dumpTypeProviders
import org.testng.annotations.Test
import withTypeProviders
import java.io.File

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class TypeProvidersCacheTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "TypeProviderLibrary"
    override val restoreNuGetPackages = true
    private val sourceFile = "TypeProviderLibrary/Caches.fs"

    private fun checkTypeProviders() {
        withOpenedEditor(project, sourceFile) {
            waitForDaemon()
            executeWithGold(testGoldFile) {
                dumpTypeProviders(it)
            }
        }
    }

    @Test
    fun checkCachesBeforeAndAfterReloading() {
        withTypeProviders {
            checkTypeProviders()

            unloadAllProjects()
            reloadAllProjects(project)

            checkTypeProviders()
        }
    }

    @Test(enabled = false)
    fun invalidation() {
        val testDirectory = File(project.basePath + "/TypeProviderLibrary/Test")

        withTypeProviders {
            withOpenedEditor(project, sourceFile) {
                waitForDaemon()

                testDirectory.deleteRecursively().shouldBeTrue()
                typeWithLatency("//")
                waitForDaemon()

                executeWithGold(File(testGoldFile.path + "_before")) {
                    dumpTypeProviders(it)
                }

                testDirectory.mkdir().shouldBeTrue()
                typeWithLatency(" ")
                waitForDaemon()

                executeWithGold(File(testGoldFile.path + "_after")) {
                    dumpTypeProviders(it)
                }
            }
        }
    }

    @Test
    fun typing() {
        withTypeProviders {
            withOpenedEditor(project, sourceFile) {
                waitForDaemon()
                typeWithLatency("//")
                waitForDaemon()
                executeWithGold(testGoldFile) {
                    dumpTypeProviders(it)
                }
            }
        }
    }
}
