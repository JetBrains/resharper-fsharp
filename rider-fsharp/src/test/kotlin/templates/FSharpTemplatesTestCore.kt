package templates

import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rider.run.configurations.project.DotNetProjectConfiguration
import com.jetbrains.rider.test.base.RiderTemplatesTestBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test

abstract class FSharpTemplatesTestCore : RiderTemplatesTestBase() {

    @Test
    fun classlibCoreTemplate() {
        var templateId = ProjectTemplateIds.currentCore.fsharp_classLibrary

        val projectName = "ClassLibrary"
        doCoreTest(templateId, projectName) { project ->
            checkSwea(project)
            checkSelectedRunConfigurationExecutionNotAllowed(project)
        }
    }

    @Test
    fun classlibNetCoreAppTemplate() {
        var templateId = ProjectTemplateIds.currentCore.fsharp_classLibrary

        val projectName = "ClassLibrary"
        doCoreTest(templateId, projectName, "netcoreapp2.1") { project ->
            checkSwea(project)
            checkSelectedRunConfigurationExecutionNotAllowed(project)
        }
    }

    @Test
    fun consoleAppCoreTemplate() {
        var templateId = ProjectTemplateIds.currentCore.fsharp_consoleApplication

        val projectName = "ConsoleApplication"
        doCoreTest(templateId, projectName) { project ->
            checkSwea(project)
            checkCanExecuteSelectedRunConfiguration(project)
            executeWithGold(configGoldFile) { printStream ->
                doTestDumpRunConfigurationsFromRunManager(project, printStream)
            }
            val output = runProgram(project)
            assert(output.contains("Hello World from F#!")) { "Wrong program output: $output" }

            val beforeRun: ExecutionEnvironment.() -> Unit = {
                this.runProfile as DotNetProjectConfiguration
                val envVars = mutableMapOf<String, String>()
                //envVars.putAll(configuration.environmentVariables)
                envVars["COREHOST_TRACE"] = "1"
                //configuration.environmentVariables = envVars
                toggleBreakpoint(project, "Program.fs", 7)
            }
            executeWithGold(debugGoldFile, getGoldFileSystemDependentSuffix()) {
                debugProgram(project, it, beforeRun,
                    test = {
                        waitForPause()
                        dumpFullCurrentData(2)
                        resumeSession()
                    },
                    outputConsumer = {},
                    exitProcessAfterTest = true
                )
            }

        }
    }

    @Test
    fun xUnitCoreTemplate() {
        var templateId = ProjectTemplateIds.currentCore.fsharp_xUnit
        
        val projectName = "UnitTestProject"
        doCoreTest(templateId, projectName) { project ->
            checkSwea(project, 0)
            checkSweaAnalysedFiles(backendLog, 1, 0, analyzedBySwea, "Tests.fs")

            // No run configuration in 2.1.402
//            checkSelectedRunConfigurationExecutionNotAllowed(project)

            runAllUnitTestsFromProject(project, projectName, 3, 3, expectedSuccessful = 3)

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
