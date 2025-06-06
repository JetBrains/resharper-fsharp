package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.jetbrains.rd.ide.model.RdDndOrderType
import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.TestProjectModelContext
import com.jetbrains.rider.test.framework.waitBackend
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File

@Test
@Solution("EmptySolution")
class FSharpProjectModelTest : ProjectModelBaseTest() {
  override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
    super.modifyOpenSolutionParams(params)
    params.restoreNuGetPackages = true
  }

  private fun moveItem(from: Array<Array<String>>, to: Array<String>, orderType: RdDndOrderType? = null) {
    // Wait for updating/refreshing items possibly queued by FSharpItemsContainerRefresher.
    waitBackend(project) {
      cutItem(project, from)
      pasteItem(project, to, orderType = orderType)
    }
  }

  private fun moveItem(from: Array<String>, to: Array<String>, orderType: RdDndOrderType? = null) {
    moveItem(arrayOf(from), to, orderType)
  }

  @Suppress("SameParameterValue")
  private fun renameItem(path: Array<String>, newName: String) {
    // Wait for updating/refreshing items possibly queued by FSharpItemsContainerRefresher.
    waitBackend(project) {
      renameItem(project, path, newName)
    }
  }

  private fun TestProjectModelContext.dump2(
    caption: String,
    checkSlnFile: Boolean,
    compareProjFile: Boolean,
    action: () -> Unit
  ) {
    dump(caption, checkSlnFile, compareProjFile, action)
    treeOutput.append(project.fcsHost.dumpSingleProjectMapping.sync(Unit))
  }

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_5)
  @Solution("FSharpProjectTree")
  fun testFSharpProjectStructure() {
    doTestDumpProjectsView {
      dump2("Init", false, false) {
      }
      dump2(
        "1. Move file 'Folder(1)/File1.fs' inside other part of the same folder after 'Folder(2)/File4.fs'",
        false,
        true
      ) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File1.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "File4.fs")
        )
      }
      dump2(
        "2. Move file 'Folder(2)/File3.fs' inside other part of the same folder before 'Folder(1)/File2.fs'",
        false,
        true
      ) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "File3.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File2.fs"), RdDndOrderType.Before
        )
      }
      dump2("3. Move file 'Folder(2)/File1.fs' before folder 'Folder(2)'", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "File1.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2"), RdDndOrderType.Before
        )
      }
      dump2("4. Move file 'File3.fs' and 'File1.fs' in folder 'Folder(2)/Sub(1)' before 'Class1.fs'", false, true) {
        moveItem(
          arrayOf(
            arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"),
            arrayOf("FSharpProjectTree", "ClassLibrary1", "File1.fs")
          ),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "Class1.fs"), RdDndOrderType.Before
        )
      }
      dump2("5. Move 'Folder/Sub/File3.fs' to project folder before EmptyFolder", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "File3.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder"), RdDndOrderType.Before
        )
      }
      dump2("6. Move 'Folder/Sub/File3.fs' to project folder after EmptyFolder", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder"), RdDndOrderType.After
        )
      }
      dump2("7. Move file 'Class2.fs' in folder 'Folder(2)' before 'Sub(2)'", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?2", "Class2.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?2"), RdDndOrderType.Before
        )
      }
      dump2("8. Move file 'Folder(1)/File2.fs' before folder 'Folder(1)/File3.fs'", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File2.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File3.fs"), RdDndOrderType.Before
        )
      }
      dump2("9. Move file 'Folder/File2.fs' before 'Folder(1)'", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File2.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1"), RdDndOrderType.Before
        )
      }
      dump2("10. Rename file 'File3.fs' to 'Foo.fs'", false, true) {
        renameItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"), "Foo.fs"
        )
      }
      dump2("11. Move file 'Foo.fs' to 'EmptyFolder(1)'", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Foo.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder?1")
        )
      }
      dump2("12. Move file 'EmptyFolder/Foo.fs' before 'EmptyFolder(1)'", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder?1", "Foo.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder?1"), RdDndOrderType.Before
        )
      }
      dump2("13. Move file 'File1.fs' and 'Class1.fs' in folder 'Folder(2)' before 'Sub(1)'", false, true) {
        moveItem(
          arrayOf(
            arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "File1.fs"),
            arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "Class1.fs")
          ),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1"), RdDndOrderType.Before
        )
      }
    }
  }

  @Test
  @Issues([Issue("RIDER-69084"), Issue("RIDER-69562")])
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_9)
  fun testFSharpDirectoryManipulation() {
    doTestDumpProjectsView {
      dump2("1. Create project", checkSlnFile = false, compareProjFile = true) {
        addProject(project, arrayOf("Solution"), "ClassLibrary", ProjectTemplates.Sdk.Net9.FSharp.classLibrary, targetFramework = "netstandard2.1")
      }
      dump2("2. Create folder 'NewFolder'", checkSlnFile = false, compareProjFile = true) {
        addNewFolder(arrayOf("Solution", "ClassLibrary"), "NewFolder")
      }
      dump2("3. Create subfolder 'NewFolder/NewSub'", checkSlnFile = false, compareProjFile = true) {
        addNewFolder(arrayOf("Solution", "ClassLibrary", "NewFolder"), "NewSub")
      }
      dump2("4. Move folder 'NewFolder/NewSub' to project root", checkSlnFile = false, compareProjFile = true) {
        moveItem(
          arrayOf("Solution", "ClassLibrary", "NewFolder", "NewSub"),
          arrayOf("Solution", "ClassLibrary")
        )
      }
      dump2("5. Delete folder 'NewSub'", checkSlnFile = false, compareProjFile = true) {
        deleteElement(arrayOf("Solution", "ClassLibrary", "NewSub"))
      }
    }
  }

  @Test
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_5)
  @Solution("FsprojWithTwoFiles")
  fun testManualFsprojChange() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }

      dump2("Move File1 and File2 lines", false, true) {
        val fsprojFile = File(activeSolutionDirectory, "ClassLibrary1/ClassLibrary1.fsproj")
        changeFileContent(project, fsprojFile) { content ->
          content
            .replace("<Compile Include=\"File2.fs\" />", "<Compile Include=\"File2.fs.tmp\" />")
            .replace("<Compile Include=\"File1.fs\" />", "<Compile Include=\"File2.fs\" />")
            .replace("<Compile Include=\"File2.fs.tmp\" />", "<Compile Include=\"File1.fs\" />")
        }
      }
    }
  }

  @Test(description = "RIDER-107198")
  @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_5)
  @Solution("SolutionWithDuplicateTargets")
  fun doNoneItemDuplicatesTest() {
    doTestDumpProjectsView {
      dump2("Init", checkSlnFile = false, compareProjFile = false) { }
    }
  }
}
