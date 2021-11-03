package com.jetbrains.rider.test.cases.templates.core

import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rider.run.configurations.project.DotNetProjectConfiguration
import com.jetbrains.rider.test.base.RiderTemplatesTestBase
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*

abstract class FSharpTemplatesTestCore : RiderTemplatesTestBase() {

    class CoreTemplateTestArgs(
        var expectedNumOfAnalyzedFiles: Int = 0,
        var expectedNumOfSkippedFiles: Int = 0,
        var breakpointFile: String = "",
        var breakpointLine: Int = 0,
        var expectedOutput: String = "",
        var targetFramework: String = ""
    )
    
    fun classLibCoreTemplate(args: CoreTemplateTestArgs) {
        val templateId = ProjectTemplateIds.currentCore.fsharp_classLibrary

        val projectName = "ClassLibrary"
        doCoreTest(templateId, projectName) { project ->
            checkSwea(project)
            checkSweaAnalysedFiles(backendLog,
                args.expectedNumOfAnalyzedFiles,
                args.expectedNumOfSkippedFiles,
                analyzedBySwea,
                "Class1.fs")
            
            checkSelectedRunConfigurationExecutionNotAllowed(project)
        }
    }

    fun classLibNetCoreAppTemplate(args: CoreTemplateTestArgs) {
        val templateId = ProjectTemplateIds.currentCore.fsharp_classLibrary

        val projectName = "ClassLibrary"
        doCoreTest(templateId, projectName, args.targetFramework) { project ->
            checkSwea(project)
            checkSweaAnalysedFiles(backendLog,
                args.expectedNumOfAnalyzedFiles,
                args.expectedNumOfSkippedFiles,
                analyzedBySwea,
                "Class1.fs")
            
            checkSelectedRunConfigurationExecutionNotAllowed(project)
        }
    }

    fun consoleAppCoreTemplate(args: CoreTemplateTestArgs) {
        val templateId = ProjectTemplateIds.currentCore.fsharp_consoleApplication

        val projectName = "ConsoleApplication"
        doCoreTest(templateId, projectName) { project ->
            checkSwea(project)
            checkSweaAnalysedFiles(backendLog,
                args.expectedNumOfAnalyzedFiles,
                args.expectedNumOfSkippedFiles,
                analyzedBySwea,
                "Program.fs")
            
            checkCanExecuteSelectedRunConfiguration(project)
            executeWithGold(configGoldFile) { printStream ->
                doTestDumpRunConfigurationsFromRunManager(project, printStream)
            }
            val output = runProgram(project)
            assert(output.contains(args.expectedOutput)) { "Wrong program output: $output" }

            val beforeRun: ExecutionEnvironment.() -> Unit = {
                this.runProfile as DotNetProjectConfiguration
                val envVars = mutableMapOf<String, String>()
                //envVars.putAll(configuration.environmentVariables)
                envVars["COREHOST_TRACE"] = "1"
                //configuration.environmentVariables = envVars
                toggleBreakpoint(project, args.breakpointFile, args.breakpointLine)
            }
            executeWithGold(debugGoldFile) {
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

    fun xUnitCoreTemplate(args: CoreTemplateTestArgs) {
        val templateId = ProjectTemplateIds.currentCore.fsharp_xUnit

        val projectName = "UnitTestProject"
        doCoreTest(templateId, projectName) { project ->
            checkSwea(project)
            checkSweaAnalysedFiles(backendLog,
                args.expectedNumOfAnalyzedFiles,
                args.expectedNumOfSkippedFiles,
                analyzedBySwea,
                "Tests.fs")

            runAllUnitTestsFromProject(project, projectName, 3, 3, expectedSuccessful = 3)

            debugUnitTests(project, debugGoldFile, {
                toggleBreakpoint(project, args.breakpointFile, args.breakpointLine)
            }) {
                waitForPause()
                dumpFullCurrentData(2)
                resumeSession()
            }
        }
    }

}
