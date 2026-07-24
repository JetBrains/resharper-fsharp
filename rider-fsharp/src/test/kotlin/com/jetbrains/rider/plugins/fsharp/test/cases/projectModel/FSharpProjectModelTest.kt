package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.jetbrains.rd.ide.model.RdDndOrderType
import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.Issue
import com.jetbrains.rider.test.annotations.report.Issues
import com.jetbrains.rider.test.junit5.base.ProjectModelBaseTest
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.TestProjectModelContext
import com.jetbrains.rider.test.framework.waitBackend
import com.jetbrains.rider.test.scriptingApi.ProjectTemplates
import com.jetbrains.rider.test.scriptingApi.addNewFolder
import com.jetbrains.rider.test.scriptingApi.addProject
import com.jetbrains.rider.test.scriptingApi.changeFileContent
import com.jetbrains.rider.test.scriptingApi.cutItem
import com.jetbrains.rider.test.scriptingApi.deleteElement
import com.jetbrains.rider.test.scriptingApi.pasteItem
import com.jetbrains.rider.test.scriptingApi.renameItem
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Tag(TeamCityTags.Plugins.FSharp)
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
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
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
  @TestSettings(sdkVersion = SdkVersion.DOT_NET_9, buildTool = BuildTool.SDK)
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
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("FsprojWithTwoFiles")
  fun testManualFsprojChange() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }

      dump2("Move File1 and File2 lines", false, true) {
        val fsprojFile = activeSolutionDirectory.resolve("ClassLibrary1/ClassLibrary1.fsproj")
        changeFileContent(project, fsprojFile) { content ->
          content
            .replace("<Compile Include=\"File2.fs\" />", "<Compile Include=\"File2.fs.tmp\" />")
            .replace("<Compile Include=\"File1.fs\" />", "<Compile Include=\"File2.fs\" />")
            .replace("<Compile Include=\"File2.fs.tmp\" />", "<Compile Include=\"File1.fs\" />")
        }
      }
    }
  }

  @Test // RIDER-107198
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("SolutionWithDuplicateTargets")
  fun doNoneItemDuplicatesTest() {
    doTestDumpProjectsView {
      dump2("Init", checkSlnFile = false, compareProjFile = false) { }
    }
  }

  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("FSharpProjectTree")
  fun testFSharpMoveFolder() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }

      // Move a whole folder part to a new position among top level items. Its items move
      // sequentially, like individual files.
      dump2("1. Move folder 'Folder(1)' after 'File3.fs'", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"), RdDndOrderType.After
        )
      }
    }
  }

  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("FSharpProjectTree")
  fun testFSharpMoveFolderJoinParts() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }

      // Move a folder part next to another part of the same folder: the parts should join
      // into a single folder.
      dump2("1. Move folder 'Folder(2)' before 'Folder(1)' (join parts)", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1"), RdDndOrderType.Before
        )
      }
    }
  }

  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("FSharpProjectTree")
  fun testFSharpMoveNestedFolder() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }

      // Move a folder part that contains a nested (split) subfolder 'Sub'. The nested structure
      // and item order must be preserved at the destination.
      dump2("1. Move folder 'Folder(2)' (with nested 'Sub') before 'EmptyFolder'", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder"), RdDndOrderType.Before
        )
      }
    }
  }

  // Move a whole non-split folder that interleaves files and a subfolder. Its items must be
  // re-added in the container (fsproj) order — File1, Sub/File2, File3 — not grouped by the
  // project model, and the emptied source folder must not be left behind.
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("InterleavedFolder")
  fun testFSharpMoveWholeFolderKeepsOrder() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }
      dump2("1. Move folder 'Folder' before 'File0.fs'", false, true) {
        moveItem(
          arrayOf("InterleavedFolder", "ClassLibrary1", "Folder"),
          arrayOf("InterleavedFolder", "ClassLibrary1", "File0.fs"), RdDndOrderType.Before
        )
      }
    }
  }

  // Reorder a non-split empty folder within the same parent. An empty folder has no files, so it
  // is moved as a whole folder (remove + re-add of its <Folder Include/> marker) rather than via
  // its files. CanMove treats a same-location folder move as a reorder rather than a name clash.
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("FSharpProjectTree")
  fun testFSharpMoveEmptyFolder() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }
      dump2("1. Move empty folder 'EmptyFolder' before 'File3.fs'", false, true) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"), RdDndOrderType.Before
        )
      }
    }
  }

  // Move an empty folder next to another empty folder. The two stay distinct (different names) and
  // reorder correctly.
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("EmptyFolders")
  fun testFSharpMoveEmptyFolderNextToAnother() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }
      dump2("1. Move empty folder 'FolderA' after 'FolderB'", false, true) {
        moveItem(
          arrayOf("EmptyFolders", "ClassLibrary1", "FolderA"),
          arrayOf("EmptyFolders", "ClassLibrary1", "FolderB"), RdDndOrderType.After
        )
      }
    }
  }

  // Move a whole folder that contains an empty subfolder. The empty subfolder's <Folder Include/>
  // marker must move with the folder's files, not be left behind.
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("FolderWithEmptySub")
  fun testFSharpMoveFolderWithEmptySubfolder() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }
      dump2("1. Move folder 'Outer' after 'Sibling.fs'", false, true) {
        moveItem(
          arrayOf("FolderWithEmptySub", "ClassLibrary1", "Outer"),
          arrayOf("FolderWithEmptySub", "ClassLibrary1", "Sibling.fs"), RdDndOrderType.After
        )
      }
    }
  }

  // Folder1 (containing nested 'Nested') is split by Folder2. Move the second Folder1 part above
  // Folder2 so it merges back into the first part.
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("SplitNested")
  fun testFSharpMergeSplitFolderWithNested() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }
      dump2("1. Move folder 'Folder1(2)' before 'Folder2'", false, true) {
        moveItem(
          arrayOf("SplitNested", "ClassLibrary1", "Folder1?2"),
          arrayOf("SplitNested", "ClassLibrary1", "Folder2"), RdDndOrderType.Before
        )
      }
    }
  }

  // Move only the nested 'Nested' part after File1. It merges with the first 'Nested' part, while
  // the second Folder1 part keeps File4 and stays split.
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("SplitNestedWithFile")
  fun testFSharpMergeNestedFolderOuterPartRemains() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }
      dump2("1. Move 'Folder1(2)/Nested' after 'Folder1(1)/Nested/File1.fs'", false, true) {
        moveItem(
          arrayOf("SplitNestedWithFile", "ClassLibrary1", "Folder1?2", "Nested"),
          arrayOf("SplitNestedWithFile", "ClassLibrary1", "Folder1?1", "Nested", "File1.fs"),
          RdDndOrderType.After
        )
      }
    }
  }

  // Move the nested 'Nested' part after File1. It merges, and the now-empty second Folder1 part is
  // removed.
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("SplitNested")
  fun testFSharpMergeNestedFolderEmptyOuterPartRemoved() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }
      dump2("1. Move 'Folder1(2)/Nested' after 'Folder1(1)/Nested/File1.fs'", false, true) {
        moveItem(
          arrayOf("SplitNested", "ClassLibrary1", "Folder1?2", "Nested"),
          arrayOf("SplitNested", "ClassLibrary1", "Folder1?1", "Nested", "File1.fs"),
          RdDndOrderType.After
        )
      }
    }
  }

  // Move the nested 'Nested' part after File1. Removing the emptied second Folder1 part makes the
  // two Folder2 parts adjacent, so both Folder1 and Folder2 split parts merge.
  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("SplitNestedTwoFolders")
  fun testFSharpMergeNestedFolderJoinsBothFolders() {
    doTestDumpProjectsView {
      dump2("Init", false, false) { }
      dump2("1. Move 'Folder1(2)/Nested' after 'Folder1(1)/Nested/File1.fs'", false, true) {
        moveItem(
          arrayOf("SplitNestedTwoFolders", "ClassLibrary1", "Folder1?2", "Nested"),
          arrayOf("SplitNestedTwoFolders", "ClassLibrary1", "Folder1?1", "Nested", "File1.fs"),
          RdDndOrderType.After
        )
      }
    }
  }
}
