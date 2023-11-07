package com.jetbrains.rider.plugins.fsharp.test.cases.fantomas

import com.intellij.openapi.project.Project
import com.intellij.util.application
import com.intellij.util.io.createFile
import com.intellij.util.io.delete
import com.intellij.util.io.write
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.fsharp.test.*
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.protocol.protocolManager
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.asserts.shouldBe
import com.jetbrains.rider.test.base.EditorTestBase
import com.jetbrains.rider.test.env.Environment
import com.jetbrains.rider.test.env.dotNetSdk
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.restoreNuGet
import java.io.File
import java.io.PrintStream
import java.nio.file.Paths
import java.time.Duration
import kotlin.io.path.*

abstract class FantomasDotnetToolTestBase : EditorTestBase() {
  override fun getSolutionDirectoryName() = "FormatCodeApp"
  override val restoreNuGetPackages = false

  private fun getDotnetCliHome() = Path(tempTestDirectory.parent, "dotnetHomeCli")
  private val fantomasNotifications = ArrayList<String>()
  protected val bundledVersion = "6.2.2.0"
  protected val globalVersion = "4.7.2.0"
  private var dotnetToolsInvalidated = false

  protected fun dumpRunOptions() = project.fcsHost.dumpFantomasRunOptions.sync(Unit)
  protected fun checkFantomasVersion(version: String) = project.fcsHost.fantomasVersion.sync(Unit).shouldBe(version)
  protected fun dumpNotifications(stream: PrintStream, expectedCount: Int) {
    stream.println("\n\nNotifications:")
    waitAndPump(project.lifetime,
      { fantomasNotifications.size >= expectedCount },
      Duration.ofSeconds(30),
      { "Didn't wait for notifications. Expected $expectedCount, but was ${fantomasNotifications.size}" })

    fantomasNotifications.forEach { stream.println(it) }
  }

  protected fun withFantomasSetting(value: String, function: () -> Unit) {
    withSetting(project, "FSharp/FSharpFantomasOptions/Location/@EntryValue", value, "AutoDetected") {
      function()
    }
  }

  private fun withDotnetToolsUpdate(function: () -> Unit) {
    dotnetToolsInvalidated = false
    function()
    flushFileChanges(project)
    waitAndPump(Duration.ofSeconds(15), { dotnetToolsInvalidated == true }, { "Dotnet tools wasn't changed." })
  }

  protected fun withFantomasLocalTool(name: String, version: String, restore: Boolean = true, function: () -> Unit) {
    val manifestFile = Paths.get(project.solutionDirectory.absolutePath, ".config", "dotnet-tools.json")
    frameworkLogger.info("Create '$manifestFile'")
    val file = manifestFile.createFile()

    try {
      withDotnetToolsUpdate {
        val toolsJson = """"$name": { "version": "$version", "commands": [ "fantomas" ] }"""
        file.write("""{ "version": 1, "isRoot": true, "tools": { $toolsJson } }""")
      }
      if (restore) {
        withDotnetToolsUpdate {
          // Trigger dotnet tools restore
          // TODO: use separate dotnet tools restore API
          restoreNuGet(project)
        }
      }
      function()
    } finally {
      withDotnetToolsUpdate {
        file.delete()
        project.fcsHost.terminateFantomasHost.sync(Unit)
      }
    }
  }

  protected fun withFantomasGlobalTool(function: () -> Unit) {
    try {
      val env = mapOf("DOTNET_CLI_HOME" to getDotnetCliHome().absolutePathString())

      withDotnetToolsUpdate {
        runProcessWaitForExit(
          Environment.dotNetSdk(testMethod.environment.sdkVersion).dotnetExecutable.toPath(),
          listOf("tool", "install", "fantomas-tool", "-g", "--version", globalVersion),
          env
        )
      }
      function()
    } finally {
      project.fcsHost.terminateFantomasHost.sync(Unit)
    }
  }

  override fun openSolution(solutionFile: File, params: OpenSolutionParams): Project {
    application.protocolManager.protocolHosts.forEach {
      editFSharpBackendSettings(it) {
        dotnetCliHomeEnvVar = getDotnetCliHome().absolutePathString()
      }
    }
    return super.openSolution(solutionFile, params)
  }

  override fun beforeDoTestWithDocuments() {
    super.beforeDoTestWithDocuments()

    fantomasNotifications.clear()

    project.fcsHost.fantomasNotificationFired.advise(testLifetimeDef.lifetime) {
      fantomasNotifications.add(it)
    }
    project.fcsHost.dotnetToolInvalidated.advise(testLifetimeDef.lifetime) {
      dotnetToolsInvalidated = true
    }

    val dotnetCliHome = getDotnetCliHome()
    if (!dotnetCliHome.exists()){
      dotnetCliHome.createDirectory()
      flushFileChanges(project)
    }
    else if (dotnetCliHome.listDirectoryEntries().any { it.name != ".nuget" }) {
      withDotnetToolsUpdate {
        dotnetCliHome.delete(true)
        dotnetCliHome.createDirectory()
        flushFileChanges(project)
      }
    }
  }
}
