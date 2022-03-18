package typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.plugins.fsharp.test.dumpTypeProviders
import com.jetbrains.rider.plugins.fsharp.test.withOutOfProcessTypeProviders
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeFalse
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class TypeProvidersCacheTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "TypeProviderLibrary"
    override val restoreNuGetPackages = true
    private val defaultSourceFile = "TypeProviderLibrary/Caches.fs"

    private fun checkTypeProviders(testGoldFile: File, sourceFile: String) {
        withOpenedEditor(project, sourceFile) {
            waitForDaemon()
            executeWithGold(testGoldFile) {
                dumpTypeProviders(it)
            }
        }
    }

    @Test
    fun checkCachesWhenProjectReloading() {
        withOutOfProcessTypeProviders {
            checkTypeProviders(File(testGoldFile.path + "_before"), defaultSourceFile)

            unloadAllProjects()
            reloadAllProjects(project)

            checkTypeProviders(File(testGoldFile.path + "_after"), defaultSourceFile)
        }
    }

    @Test
    fun invalidation() {
        val testDirectory = File(project.basePath + "/TypeProviderLibrary/Test")

        withOutOfProcessTypeProviders {
            withOpenedEditor(project, defaultSourceFile) {
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
        withOutOfProcessTypeProviders {
            withOpenedEditor(project, defaultSourceFile) {
                waitForDaemon()
                typeWithLatency("//")
                checkTypeProviders(testGoldFile, defaultSourceFile)
            }
        }
    }

    @Test
    fun projectsWithEqualProviders() {
        withOutOfProcessTypeProviders {
            withOpenedEditor(project, "TypeProviderLibrary/Library.fs") {
                waitForDaemon()
            }
            withOpenedEditor(project, "TypeProviderLibrary2/Library.fs") {
                waitForDaemon()
                checkTypeProviders(testGoldFile, defaultSourceFile)
            }
        }
    }

    @Test(description = "RIDER-73091")
    fun script() {
        withOutOfProcessTypeProviders {
            withOpenedEditor(project, defaultSourceFile) {
                waitForDaemon()
                markupAdapter.hasErrors.shouldBeFalse()
            }
            checkTypeProviders(File(testGoldFile.path + "_before"), "TypeProviderLibrary/Script.fsx")

            unloadAllProjects()
            reloadAllProjects(project)

            checkTypeProviders(File(testGoldFile.path + "_after"), "TypeProviderLibrary/Script.fsx")
        }
    }
}
