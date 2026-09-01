package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.intellij.openapi.actionSystem.IdeActions
import com.intellij.openapi.command.WriteCommandAction
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.fileEditor.ex.FileEditorManagerEx
import com.intellij.openapi.project.Project
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rider.daemon.util.hasErrors
import com.jetbrains.rider.editors.getProjectModelId
import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.plugins.fsharp.test.framework.withNonFSharpProjectReferences
import com.jetbrains.rider.projectView.workspace.containingProjectEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.projectView.workspace.getProjectModelEntity
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.junit5.base.PerTestProjectModelTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.framework.waitBackend
import com.jetbrains.rider.test.scriptingApi.addReference
import com.jetbrains.rider.test.scriptingApi.callAction
import com.jetbrains.rider.test.scriptingApi.changeFileSystem
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.scriptingApi.reloadProject
import com.jetbrains.rider.test.scriptingApi.typeFromOffset
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.unloadProject
import com.jetbrains.rider.test.scriptingApi.waitForDaemonCloseAllOpenEditors
import com.jetbrains.rider.test.scriptingApi.waitForNextDaemon
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.util.idea.syncFromBackend
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.io.PrintStream
import java.time.Duration
import kotlin.io.path.writeText

@Tag(TeamCityTags.Plugins.FSharp.General)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("EmptySolution")
class FcsModuleReaderTest : PerTestProjectModelTestBase() {
  override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
    super.modifyOpenSolutionParams(params)
    params.restoreNuGetPackages = true
    params.backendLoadedTimeout = Duration.ofMinutes(20)
  }

  private fun EditorImpl.assertFcsStampAndReferencedProjectNames(
    editorImpl: EditorImpl,
    expectedReferencedProjects: List<String>
  ) {
    val project = project!!
    val workspaceModel = WorkspaceModel.getInstance(project)
    val fcsHost = project.fcsHost

    val fileProjectModelEntity = workspaceModel.getProjectModelEntity(editorImpl.getProjectModelId())
    val projectProjectModelId = fileProjectModelEntity?.containingProjectEntity()?.getId(project)!!

    val referencedProjects = fcsHost.dumpFcsReferencedProjects.syncFromBackend(projectProjectModelId, project)!!
    assert(expectedReferencedProjects == referencedProjects)
  }

  private fun openFsFileDumpModuleReader(
    printStream: PrintStream,
    caption: String,
    hasErrors: Boolean,
    expectedReferencedProjects: List<String>
  ) {
    val project = project
    withOpenedEditor("FSharpProject/Library.fs") {
      waitForNextDaemon()
      assert(markupAdapter.hasErrors == hasErrors)
      assertFcsStampAndReferencedProjectNames(this, expectedReferencedProjects)
      dumpModuleReader(printStream, caption, project)
    }
    waitForDaemonCloseAllOpenEditors(project)
  }

  private fun dumpModuleReader(
    printStream: PrintStream,
    caption: String,
    project: Project
  ) {
    printStream.appendLine("===================")
    printStream.println(caption)
    printStream.println()
    printStream.println(project.fcsHost.dumpFcsModuleReader.syncFromBackend(Unit, project))
  }

  // Copied from EditorTestBase
  private fun waitForEditorSwitch(targetFileName: String, hostTimeout: Duration = Duration.ofSeconds(20)) {
    frameworkLogger.info("Waiting for editor switch")
    val instanceEx = FileEditorManagerEx.getInstanceEx(project)

    pumpMessages(hostTimeout) {
      val name = instanceEx.selectedEditor?.file?.name
      name == targetFileName
    }

    val name = instanceEx.selectedEditor?.file?.name

    assert(name == targetFileName) { "Editor should be switched. Current editor: $name" }
    frameworkLogger.info("Editor switched to $targetFileName")
  }

  @Solution("ProjectReferencesCSharp")
  @Test
  fun testUnloadReloadCSharp() {
    executeWithGold(testGoldFile) {
      withNonFSharpProjectReferences {
        assertAllProjectsWereLoaded()
        dumpModuleReader(it, "Init", project)

        openFsFileDumpModuleReader(it, "1. Open F# file", true, emptyList())

        addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        dumpModuleReader(it, "2. Add reference", project)

        openFsFileDumpModuleReader(it, "3. Open F# file", false, listOf("CSharpProject"))

        unloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "4. Unload C# project", project)

        openFsFileDumpModuleReader(it, "5. Open F# file", true, emptyList())

        reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "6. Reload C# project", project)

        openFsFileDumpModuleReader(it, "7. Open F# file", false, listOf("CSharpProject"))
      }
    }
  }

  @Solution("ProjectReferencesCSharp")
  @Test
  fun testTypeInsideClassUnloadReload() {
    executeWithGold(testGoldFile) {
      withNonFSharpProjectReferences {
        assertAllProjectsWereLoaded()
        openFsFileDumpModuleReader(it, "Init", true, emptyList())

        addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        dumpModuleReader(it, "1. Add reference", project)

        openFsFileDumpModuleReader(it, "2. Open F# file", false, listOf("CSharpProject"))

        withOpenedEditor("CSharpProject/Class1.cs") {
          typeFromOffset(" ", 75)
          waitForNextDaemon()
        }

        waitForDaemonCloseAllOpenEditors(project)
        dumpModuleReader(it, "3. Type inside C# file", project)

        openFsFileDumpModuleReader(it, "4. Open F# file", false, listOf("CSharpProject"))

        unloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "5. Unload C# project", project)

        openFsFileDumpModuleReader(it, "6. Open F# file", true, emptyList())

        reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "7. Reload C# project", project)

        openFsFileDumpModuleReader(it, "8. Open F# file", false, listOf("CSharpProject"))

        withOpenedEditor("CSharpProject/Class1.cs") {
          typeFromOffset(" ", 75)
        }

        waitForDaemonCloseAllOpenEditors(project)
        dumpModuleReader(it, "9. Type inside C# file", project)

        unloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "10. Unload C# project", project)

        openFsFileDumpModuleReader(it, "11. Open F# file", true, emptyList())

        reloadProject(arrayOf("ProjectReferencesCSharp", "CSharpProject"))
        dumpModuleReader(it, "12. Reload C# project", project)

        openFsFileDumpModuleReader(it, "13. Open F# file", false, listOf("CSharpProject"))
      }
    }
  }

  @Solution("ProjectReferencesCSharp")
  @Test
  fun testTypeOutsideClassUnloadReload() {
    executeWithGold(testGoldFile) {
      withNonFSharpProjectReferences {
        assertAllProjectsWereLoaded()
        openFsFileDumpModuleReader(it, "Init", true, emptyList())

        addReference(project, arrayOf("ProjectReferencesCSharp", "FSharpProject"), "<CSharpProject>")
        dumpModuleReader(it, "1. Add reference", project)

        openFsFileDumpModuleReader(it, "2. Open F# file", false, listOf("CSharpProject"))

        withOpenedEditor("CSharpProject/Class1.cs") {
          typeFromOffset(" ", 129)
        }

        waitForDaemonCloseAllOpenEditors(project)
        dumpModuleReader(it, "3. Type inside C# file", project)

        openFsFileDumpModuleReader(it, "4. Open F# file", false, listOf("CSharpProject"))
      }
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testLoadReferenced() {
    executeWithGold(testGoldFile) {
      withNonFSharpProjectReferences {
        assertAllProjectsWereLoaded()
        openFsFileDumpModuleReader(it, "Init", false, listOf("CSharpProject"))

        waitForDaemonCloseAllOpenEditors(project)
        unloadProject(arrayOf("ProjectReferencesCSharp2", "CSharpProject"))
        dumpModuleReader(it, "2. Unload C# project", project)

        waitForDaemonCloseAllOpenEditors(project)
        openFsFileDumpModuleReader(it, "3. Open F#", true, emptyList())
      }
    }
  }

  // The open makes FCS import the referenced C# module.
  private fun openFsFileAssertErrors(caption: String, hasErrors: Boolean) {
    withOpenedEditor("FSharpProject/Library.fs") {
      waitForNextDaemon()
      assert(markupAdapter.hasErrors == hasErrors) {
        "$caption: expected hasErrors=$hasErrors in Library.fs, got ${markupAdapter.hasErrors}"
      }
    }
    waitForDaemonCloseAllOpenEditors(project)
  }

  // A file write waits for the backend and the project model, so write one step's files together.
  private fun writeSolutionFiles(vararg files: Pair<String, String>) {
    changeFileSystem(project) {
      for ((relativePath, text) in files) {
        withIOFile(activeSolutionDirectory, relativePath).writeText(text)
      }
    }
  }

  private fun writeSolutionFile(projectName: String, fileName: String, text: String) {
    writeSolutionFiles("$projectName/$fileName" to text)
  }

  // A document change skips the file system and the project model, so it costs far less.
  private fun editCSharpFile(oldText: String, newText: String) {
    withOpenedEditor("CSharpProject/Class1.cs") {
      val offset = document.text.indexOf(oldText)
      assert(offset >= 0) { "'$oldText' is not in Class1.cs" }

      waitBackend(project!!) {
        WriteCommandAction.runWriteCommandAction(project) {
          document.replaceString(offset, offset + oldText.length, newText)
        }
      }
    }
    waitForDaemonCloseAllOpenEditors(project)
  }

  private fun writeCSharpAndFSharp(csharpText: String, fsharpExpression: String) {
    writeSolutionFiles(
      "CSharpProject/Class1.cs" to csharpText,
      "FSharpProject/Library.fs" to fsharpModule(fsharpExpression)
    )
  }

  private fun fsharpModule(expression: String) =
    """
    module FSharpProject

    let x = $expression
    """.trimIndent()

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testAddTypeToReferencedProject() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()
      openFsFileAssertErrors("Init", false)

      writeSolutionFile(
        "CSharpProject", "Class1.cs",
        """
        using System;

        namespace CSharpProject
        {
            public class CSharpClass
            {
                public static readonly int Prop = 123;
            }

            public class CSharpClass2
            {
                public static readonly int Prop = 456;
            }
        }
        """.trimIndent()
      )

      writeSolutionFile(
        "FSharpProject", "Library.fs",
        """
        module FSharpProject

        let x = CSharpProject.CSharpClass2.Prop
        """.trimIndent()
      )

      openFsFileAssertErrors("After adding a C# type", false)

      // A control: a type no one added must stay unresolved.
      writeSolutionFile(
        "FSharpProject", "Library.fs",
        """
        module FSharpProject

        let x = CSharpProject.CSharpClass3.Prop
        """.trimIndent()
      )

      openFsFileAssertErrors("Control: a missing C# type", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testAddMemberToReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()
      openFsFileAssertErrors("Init", false)

      writeSolutionFile(
        "CSharpProject", "Class1.cs",
        """
        using System;

        namespace CSharpProject
        {
            public class CSharpClass
            {
                public static readonly int Prop = 123;

                public static int Method() => 456;
            }
        }
        """.trimIndent()
      )

      writeSolutionFile(
        "FSharpProject", "Library.fs",
        """
        module FSharpProject

        let x = CSharpProject.CSharpClass.Method()
        """.trimIndent()
      )

      openFsFileAssertErrors("After adding a C# member", false)
    }
  }

  private fun writeCSharpClassWithFieldModifiers(modifiers: String) {
    writeSolutionFile(
      "CSharpProject", "Class1.cs",
      """
      using System;

      namespace CSharpProject
      {
          public class CSharpClass
          {
              $modifiers int Prop = 123;
          }
      }
      """.trimIndent()
    )
  }

  // RIDER-141924
  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeFieldModifierInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      openFsFileAssertErrors("Init", false)

      writeCSharpClassWithFieldModifiers("private static readonly")
      openFsFileAssertErrors("After the field became private", true)

      writeCSharpClassWithFieldModifiers("public static readonly")
      openFsFileAssertErrors("After the field became public", false)
    }
  }

  // RIDER-141924
  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeFieldStaticModifierInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()
      openFsFileAssertErrors("Init", false)

      writeCSharpClassWithFieldModifiers("public readonly")
      openFsFileAssertErrors("After the field became an instance field", true)
    }
  }

  private fun csharpClassWithProperty(modifiers: String, propertyType: String) =
    csharpClassWithMember("$modifiers $propertyType Property => default;")

  private fun writeCSharpClassWithProperty(modifiers: String, propertyType: String) {
    writeSolutionFile("CSharpProject", "Class1.cs", csharpClassWithProperty(modifiers, propertyType))
  }

  private fun setUpCSharpProperty(modifiers: String, propertyType: String) {
    writeSolutionFiles(
      "CSharpProject/Class1.cs" to csharpClassWithProperty(modifiers, propertyType),
      "FSharpProject/Library.fs" to
        """
        module FSharpProject

        let x: int = CSharpProject.CSharpClass.Property
        """.trimIndent()
    )
  }

  // RIDER-141924
  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangePropertyModifierInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      setUpCSharpProperty("public static", "int")

      openFsFileAssertErrors("Init", false)

      writeCSharpClassWithProperty("public", "int")
      openFsFileAssertErrors("After the property became an instance property", true)
    }
  }

  // RIDER-141924
  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangePropertyTypeInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      setUpCSharpProperty("public static", "int")
      openFsFileAssertErrors("Init", false)

      writeCSharpClassWithProperty("public static", "string")
      openFsFileAssertErrors("After the property type changed", true)
    }
  }

  // The count guard compared a length with itself, so it never failed.
  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testAddPropertyToReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      setUpCSharpProperty("public static", "int")
      openFsFileAssertErrors("Init", false)

      writeSolutionFile(
        "CSharpProject", "Class1.cs",
        """
        using System;

        namespace CSharpProject
        {
            public class CSharpClass
            {
                public static readonly int Prop = 123;

                public static int Property => default;

                public static int Property2 => default;
            }
        }
        """.trimIndent()
      )

      writeSolutionFile(
        "FSharpProject", "Library.fs",
        """
        module FSharpProject

        let x: int = CSharpProject.CSharpClass.Property2
        """.trimIndent()
      )

      openFsFileAssertErrors("After adding a C# property", false)
    }
  }

  // RIDER-141924
  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeTypeModifierInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      openFsFileAssertErrors("Init", false)

      writeSolutionFile(
        "CSharpProject", "Class1.cs",
        """
        using System;

        namespace CSharpProject
        {
            internal class CSharpClass
            {
                public static readonly int Prop = 123;
            }
        }
        """.trimIndent()
      )

      openFsFileAssertErrors("After the type became internal", true)
    }
  }

  private fun csharpClassWithMember(member: String) =
    """
    using System;

    namespace CSharpProject
    {
        public class CSharpClass
        {
            public static readonly int Prop = 123;

            $member
        }
    }
    """.trimIndent()

  private fun writeCSharpClassWithMethod(member: String) {
    writeSolutionFile("CSharpProject", "Class1.cs", csharpClassWithMember(member))
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeDuplicatePropertyInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeSolutionFiles(
        "CSharpProject/Class1.cs" to
          """
          using System;

          namespace CSharpProject
          {
              public class CSharpClass
              {
                  public static int Duplicate => 1;
                  public static int Duplicate => 2;

                  public static int Other => 3;
              }
          }
          """.trimIndent(),
        "FSharpProject/Library.fs" to fsharpModule("CSharpProject.CSharpClass.Other")
      )
      openFsFileAssertErrors("Init", false)

      editCSharpFile("public static int Other => 3;", "private static int Other => 3;")
      openFsFileAssertErrors("After the property became private", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeDuplicateMethodInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeSolutionFiles(
        "CSharpProject/Class1.cs" to
          """
          using System;

          namespace CSharpProject
          {
              public class CSharpClass
              {
                  public static int Duplicate(int i) => 1;
                  public static int Duplicate(int i) => 2;

                  public static int Other() => 3;
              }
          }
          """.trimIndent(),
        "FSharpProject/Library.fs" to fsharpModule("CSharpProject.CSharpClass.Other()")
      )
      openFsFileAssertErrors("Init", false)

      editCSharpFile("public static int Other() => 3;", "private static int Other() => 3;")
      openFsFileAssertErrors("After the method became private", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testResolveUnresolvedReturnTypeInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(
        csharpClassWithMember("public static Missing Method() => null;"),
        "CSharpProject.CSharpClass.Method().Value"
      )
      openFsFileAssertErrors("Init: the return type is unresolved", true)

      editCSharpFile(
        "public static Missing Method() => null;",
        "public static Missing Method() => null;\n    }\n\n    public class Missing { public int Value = 1; }\n\n    public class Unused {"
      )
      openFsFileAssertErrors("After the return type resolved", false)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeVoidReturnTypeInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(
        csharpClassWithMember("public static void Method() { }"),
        "(CSharpProject.CSharpClass.Method(): unit)"
      )
      openFsFileAssertErrors("Init", false)

      editCSharpFile("public static void Method() { }", "public static int Method() => 1;")
      openFsFileAssertErrors("After the void return became an int", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeTypeParameterPositionInReferencedMethod() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(
        csharpClassWithMember("public static void Generic<T, U>(U u) { }"),
        "CSharpProject.CSharpClass.Generic<int, string>(\"a\")"
      )
      openFsFileAssertErrors("Init", false)

      editCSharpFile("Generic<T, U>(U u)", "Generic<T, U>(T t)")
      openFsFileAssertErrors("After the type parameter position changed", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeArrayRankInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(
        csharpClassWithMember("public static int[] Method() => null;"),
        "(CSharpProject.CSharpClass.Method(): int[])"
      )
      openFsFileAssertErrors("Init", false)

      editCSharpFile("public static int[] Method()", "public static int[,] Method()")
      openFsFileAssertErrors("After the array rank changed", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeBaseTypeInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeSolutionFiles(
        "CSharpProject/Class1.cs" to
          """
          using System;

          namespace CSharpProject
          {
              public class Base1 { }
              public class Base2 { }

              public class Derived : Base1 { }
          }
          """.trimIndent(),
        "FSharpProject/Library.fs" to fsharpModule("CSharpProject.Derived() :> CSharpProject.Base1")
      )
      openFsFileAssertErrors("Init", false)

      editCSharpFile("Derived : Base1", "Derived : Base2")
      openFsFileAssertErrors("After the base type changed", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeInterfaceInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeSolutionFiles(
        "CSharpProject/Class1.cs" to
          """
          using System;

          namespace CSharpProject
          {
              public interface IFirst { }
              public interface ISecond { }

              public class Impl : IFirst { }
          }
          """.trimIndent(),
        "FSharpProject/Library.fs" to fsharpModule("CSharpProject.Impl() :> CSharpProject.IFirst")
      )
      openFsFileAssertErrors("Init", false)

      editCSharpFile("Impl : IFirst", "Impl : ISecond")
      openFsFileAssertErrors("After the interface changed", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeEventTypeInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      // F# gives an event `Add` only for a delegate that takes a sender and an argument.
      writeCSharpAndFSharp(
        csharpClassWithMember("public static event EventHandler<EventArgs> Event;"),
        "CSharpProject.CSharpClass.Event.Add(fun (args: System.EventArgs) -> ignore args)"
      )
      openFsFileAssertErrors("Init", false)

      editCSharpFile("EventHandler<EventArgs> Event", "EventHandler<ConsoleCancelEventArgs> Event")
      openFsFileAssertErrors("After the event type changed", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testAddObsoleteAttributeToReferencedMethod() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(
        csharpClassWithMember("public static int Method() => 1;"),
        "CSharpProject.CSharpClass.Method()"
      )
      openFsFileAssertErrors("Init", false)

      editCSharpFile(
        "public static int Method() => 1;",
        "[Obsolete(\"gone\", true)] public static int Method() => 1;"
      )
      openFsFileAssertErrors("After the method became obsolete", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeParameterNameInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(csharpClassWithMember("public static int Method(int value) => value;"), "CSharpProject.CSharpClass.Method(value = 1)")
      openFsFileAssertErrors("Init", false)

      editCSharpFile("int value) => value", "int other) => other")
      openFsFileAssertErrors("After the parameter name changed", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeParameterRefKindInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(csharpClassWithMember("public static void Method(out int value) { value = 1; }"), "CSharpProject.CSharpClass.Method()")
      openFsFileAssertErrors("Init", false)

      // An `out` parameter is implicit in F#, a `ref` parameter is not.
      writeCSharpClassWithMethod("public static void Method(ref int value) { value = 1; }")
      openFsFileAssertErrors("After `out` became `ref`", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeGenericConstraintInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(csharpClassWithMember("public static T Generic<T>(T t) where T : class => t;"), "CSharpProject.CSharpClass.Generic<string>(\"a\")")
      openFsFileAssertErrors("Init", false)

      writeCSharpClassWithMethod("public static T Generic<T>(T t) where T : struct => t;")
      openFsFileAssertErrors("After the constraint became `struct`", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeMethodReturnTypeInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(csharpClassWithMember("public static int Method() => 1;"), "(CSharpProject.CSharpClass.Method(): int)")
      openFsFileAssertErrors("Init", false)

      writeCSharpClassWithMethod("public static string Method() => \"a\";")
      openFsFileAssertErrors("After the return type changed", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeMethodStaticModifierInReferencedType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(csharpClassWithMember("public static int Method() => 1;"), "CSharpProject.CSharpClass.Method()")
      openFsFileAssertErrors("Init", false)

      writeCSharpClassWithMethod("public int Method() => 1;")
      openFsFileAssertErrors("After the method became an instance method", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testChangeGenericMethodParameterType() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(csharpClassWithMember("public static T Generic<T>(T t, int i) => t;"), "CSharpProject.CSharpClass.Generic<string>(\"a\", 1)")
      openFsFileAssertErrors("Init", false)

      writeCSharpClassWithMethod("public static T Generic<T>(T t, string s) => t;")
      openFsFileAssertErrors("After the parameter type changed", true)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testAddTypeParameterToReferencedMethod() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeCSharpAndFSharp(csharpClassWithMember("public static T Generic<T>(T t) => t;"), "CSharpProject.CSharpClass.Generic<string>(\"a\")")
      openFsFileAssertErrors("Init", false)

      writeCSharpClassWithMethod("public static T Generic<T, U>(T t) => t;")
      openFsFileAssertErrors("After the method got a second type parameter", true)
    }
  }

  // `mkGenericParamDefs` reverses the list, so two type parameters catch a wrong order.
  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testGenericTypeWithTwoTypeParameters() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      writeSolutionFile(
        "CSharpProject", "Class1.cs",
        """
        using System;

        namespace CSharpProject
        {
            public class CSharpClass
            {
                public static readonly int Prop = 123;
            }

            public class Pair<TFirst, TSecond>
            {
                public static TFirst GetFirst(TFirst first, TSecond second) => first;
            }
        }
        """.trimIndent()
      )

      // A reversed type parameter list gives a string here.
      writeSolutionFile(
        "FSharpProject", "Library.fs",
        """
        module FSharpProject

        let x: int = CSharpProject.Pair<int, string>.GetFirst(1, "a")
        """.trimIndent()
      )

      openFsFileAssertErrors("A type with two type parameters", false)
    }
  }

  @Solution("ProjectReferencesCSharp2")
  @Test
  fun testGotoUsagesFromCSharp() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()
      withOpenedEditor("CSharpProject/Class1.cs", "Class1.cs") {
        waitForNextDaemon()
        callAction(IdeActions.ACTION_GOTO_DECLARATION)
        waitForEditorSwitch("Library.fs")
      }
    }
  }

  @Solution("ProjectReferencesCSharp3")
  @Test
  fun testGotoUsagesFromCSharpChangeCSharp() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()
      withOpenedEditor("CSharpProject/Class1.cs", "Class1.cs") {
        typeWithLatency("1")
        waitForNextDaemon()
        callAction(IdeActions.ACTION_GOTO_DECLARATION)
        waitForEditorSwitch("Library.fs")
      }
    }
  }

  @Solution("ProjectReferencesCSharp3")
  @Test
  fun testGotoUsagesFromCSharpChangeCSharp2() {
    withNonFSharpProjectReferences {
      assertAllProjectsWereLoaded()

      withOpenedEditor("FSharpProject/Library.fs") {
        waitForNextDaemon()
      }

      waitForDaemonCloseAllOpenEditors(project)

      withOpenedEditor("CSharpProject/Class1.cs", "Class1.cs") {
        typeWithLatency("1")
        waitForNextDaemon()
        callAction(IdeActions.ACTION_GOTO_DECLARATION)
        waitForEditorSwitch("Library.fs")
      }
    }
  }
}
