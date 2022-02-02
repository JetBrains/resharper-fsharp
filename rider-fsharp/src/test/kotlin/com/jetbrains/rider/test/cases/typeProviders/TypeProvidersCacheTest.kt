package com.jetbrains.rider.test.cases.typeProviders

import com.jetbrains.rdclient.testFramework.executeWithGold
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.test.framework.dumpTypeProviders
import com.jetbrains.rider.test.framework.withOutOfProcessTypeProviders
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.reloadAllProjects
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.unloadAllProjects
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test
import java.io.File

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class TypeProvidersCacheTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "TypeProviderLibrary"
    override val restoreNuGetPackages = true
    private val sourceFile = "TypeProviderLibrary/Caches.fs"

    private fun checkTypeProviders(testGoldFile: File) {
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
            checkTypeProviders(File(testGoldFile.path + "_before"))

            unloadAllProjects()
            reloadAllProjects(project)

            checkTypeProviders(File(testGoldFile.path + "_after"))
        }
    }

    @Test
    fun invalidation() {
        val testDirectory = File(project.basePath + "/TypeProviderLibrary/Test")

        withOutOfProcessTypeProviders {
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
        withOutOfProcessTypeProviders {
            withOpenedEditor(project, sourceFile) {
                waitForDaemon()
                typeWithLatency("//")
                checkTypeProviders(testGoldFile)
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
                checkTypeProviders(testGoldFile)
            }
        }
    }
}
