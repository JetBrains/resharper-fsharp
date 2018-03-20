package templates

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rider.debugger.DotNetDebugRunner
import com.jetbrains.rider.diagnostics.LogTraceScenarios
import com.jetbrains.rider.run.mono.MonoDebugProfileState
import com.jetbrains.rider.test.BaseTestWithSolutionBase
import com.jetbrains.rider.test.annotations.TestToolset
import com.jetbrains.rider.test.annotations.ToolsetVersion
import com.jetbrains.rider.test.framework.*
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.util.idea.ReSharperHostException
import org.testng.annotations.AfterClass
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File
import java.nio.file.Paths


@Test
abstract class FSharpTemplatesTestCore : BaseTestWithSolutionBase() {

    protected abstract fun runWithDotNetCliVersion(): String

    private val editorGoldFile: File
        get() = File(testCaseGoldDirectory, testCaseName + "_opened")
    protected val configGoldFile: File
        get() = File(testCaseGoldDirectory, testCaseName + "_rcf")
    protected val debugGoldFile: File
        get() = File(testCaseGoldDirectory, testCaseName + "_debug")
    private val binFolderContentGoldFile: File
        get() = File(testCaseGoldDirectory, testCaseName + "_bin")
    private val fileListAbsoluteFilesGoldFile: File
        get() = File(testCaseGoldDirectory, testCaseName + "_abs")

    override val traceCategories: List<String>
        get() = arrayListOf(
                "#${DotNetDebugRunner::class.java.name}",
                "#${MonoDebugProfileState::class.java.name}",
                "JetBrains.ProjectModel.ProjectsHost.SolutionHost",
                "JetBrains.ReSharper.Host.Features.ProjectModel.View",
                *super.traceCategories.toTypedArray())

    override val traceScenarios: Set<LogTraceScenarios>
        get() = setOf(LogTraceScenarios.Caches)

    @BeforeMethod
    fun setUpBeforeMethod() {
        dotnetCoreCliVersion = runWithDotNetCliVersion()
        dotnetCoreCliPath = getDotNetCorePathFromTestData(dotnetCoreCliVersion).canonicalPath
        setUpDotNetCoreCliPath(dotnetCoreCliPath)

        //set invalid mono path, cause all core projects should use only core sdk
        setUpCustomMonoPath(dotnetCoreCliPath)
    }

    @AfterClass
    fun resetValuesAfterClass() {
        dotnetCoreCliVersion = "2.0.0"
        dotnetCoreCliPath = getDotNetCorePathFromTestData(dotnetCoreCliVersion).canonicalPath
        setUpDotNetCoreCliPath(dotnetCoreCliPath)

        setUpCustomMonoPath("")
    }

    @TestToolset(ToolsetVersion.toolset15CoreSDK)
    fun classlibCoreTemplate() {
        val projectName = "ClassLibrary"
        doCoreTest(ProjectTemplateIds.Core.fsharp_classLibrary, projectName) { project ->
            checkSwea(project)
            checkSelectedRunConfigurationExecutionNotAllowed(project)
        }
    }

    @TestToolset(ToolsetVersion.toolset15CoreSDK)
    fun classlibNetCoreAppTemplate() {
        val projectName = "ClassLibrary"
        doCoreTest(ProjectTemplateIds.Core.fsharp_classLibrary, projectName, "netcoreapp2.0") { project ->
            checkSwea(project)
            checkSelectedRunConfigurationExecutionNotAllowed(project)
        }
    }



    @TestToolset(ToolsetVersion.toolset15CoreSDK)
    fun consoleAppCoreTemplate() {
        val projectName = "ConsoleApplication"
        val programFs = activeSolutionDirectory.resolve(projectName).resolve("Program.fs")
        doCoreTest(ProjectTemplateIds.Core.fsharp_consoleApplication, projectName) { project ->
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


    @TestToolset(ToolsetVersion.toolset15CoreSDK)
    fun xUnitCoreTemplate() {
        val projectName = "UnitTestProject"
        doCoreTest(ProjectTemplateIds.Core.fsharp_xUnit, projectName) { project ->
            checkSwea(project, "", 1)
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

    private fun doCoreTest(templateName: String, projectName : String, targetFramework: String? = null, function: (Project) -> Unit) {
        val params = OpenSolutionParams()
        params.restoreNuGetPackages = true //it's always true in withSolutionOpenedFromProject
        params.backendLoadedTimeoutInSec = 120
        params.waitForCaches = true

        try {
            withSolutionOpenedFromTemplate(templateName, projectName, targetFramework, params) { project ->
                executeWithGold(editorGoldFile) {
                    dumpOpenedDocument(it, project) //every new dotnet version caret pos is changed
                }

                testProjectModel(testGoldFile, project) {
                    dump("Opened", project, activeSolutionDirectory, false, false) {} //contains close editors
                }

                //checkBuildAndSwea(project, "", 0) //until all tests will be ok
                buildSolutionWithReSharperBuild(project)
                checkThatBuildArtifactsExist(project)
                dumpAllProjectFilesListByContainsPath(project, Paths.get("bin"), binFolderContentGoldFile)
                dumpAllFileListAbsolute(project, fileListAbsoluteFilesGoldFile)

                checkThatSolutionWasRestoredOnce(backendLog)

                function(project)
            }
        } finally {
            closeSolution(true)
        }
    }

}
