package templates

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.RiderTemplatesTestCoreBase
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test

@Test
abstract class FSharpTemplatesTestCore : RiderTemplatesTestCoreBase() {

    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_15_CORE)
    fun classlibCoreTemplate() {
        var templateId = ProjectTemplateIds.Core.fsharp_classLibrary
        if (runWithDotNetCliVersion().startsWith("2.1"))
            templateId = ProjectTemplateIds.Core.fsharp_classLibrary21

        val projectName = "ClassLibrary"
        doCoreTest(templateId, projectName) { project ->
            checkSwea(project)
            checkSelectedRunConfigurationExecutionNotAllowed(project)
        }
    }

    @Test(enabled = false) //RIDER-14467
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_15_CORE)
    fun classlibNetCoreAppTemplate() {
        val projectName = "ClassLibrary"
        doCoreTest(ProjectTemplateIds.Core.fsharp_classLibrary, projectName, "netcoreapp2.0") { project ->
            checkSwea(project)
            checkSelectedRunConfigurationExecutionNotAllowed(project)
        }
    }


    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_15_CORE)
    fun consoleAppCoreTemplate() {
        var templateId = ProjectTemplateIds.Core.fsharp_consoleApplication
        if (runWithDotNetCliVersion().startsWith("2.1"))
            templateId = ProjectTemplateIds.Core.fsharp_consoleApplication21

        val projectName = "ConsoleApplication"
        //val programFs = activeSolutionDirectory.resolve(projectName).resolve("Program.fs")
        doCoreTest(templateId, projectName) { project ->
            checkSwea(project)
            checkCanExecuteSelectedRunConfiguration(project)
            executeWithGold(configGoldFile) { printStream ->
                doTestDumpRunConfigurationsFromRunManager(project, printStream)
            }
            val output = runProgram(project)
            assert(output.contains("Hello World from F#!")) { "Wrong program output: $output" }

            //todo enable after move ScriptingAPI.Debug.Temp to ScriptingAPI
            /*val beforeRun: ExecutionEnvironment.() -> Unit = {
                this.runProfile as DotNetProjectConfiguration
                val envVars = mutableMapOf<String, String>()
                //envVars.putAll(configuration.environmentVariables)
                envVars["COREHOST_TRACE"] = "1"
                //configuration.environmentVariables = envVars
                toggleBreakpoint(project, programFs.toVirtualFile(true)!!, 7)
            }
            debugProgram(project, debugGoldFile, beforeRun) {
                waitForPause()
                dumpFullCurrentData(2)
                resumeSession()

                // waiting for exit console app
                if (!this.session.debugProcess.processHandler.waitFor(5000)) {
                    logger.warn("Console app hasn't terminated for 5 seconds.")
                }
            }*/

        }
    }


    @Test(enabled = false) //RIDER-14467
    @TestEnvironment(toolset = ToolsetVersion.TOOLSET_15_CORE)
    fun xUnitCoreTemplate() {
        val projectName = "UnitTestProject"
        doCoreTest(ProjectTemplateIds.Core.fsharp_xUnit, projectName) { project ->
            checkSwea(project, 1)
            checkSweaAnalysedFiles(backendLog, 2, 0, "Tests.fs")

            checkSelectedRunConfigurationExecutionNotAllowed(project)

            runAllUnitTestsFromProject(project, projectName, 2, 2, expectedSuccessful = 2)

            //todo enable after move ScriptingAPI.Debug.Temp to ScriptingAPI
            /*val testsCs = activeSolutionDirectory.resolve(projectName).resolve("UnitTest1.cs").toVirtualFile(true)!!
            debugUnitTests(project, debugGoldFile, {
                toggleBreakpoint(project, testsCs, 10)
            }) {
                waitForPause()
                dumpFullCurrentData(2)
                resumeSession()
            }*/

        }
    }

}
