package com.jetbrains.rider.plugins.fsharp.test.cases.projectModel

import com.jetbrains.rd.ide.model.RdDndOrderType
import com.jetbrains.rider.plugins.fsharp.test.framework.fcsHost
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.Issue
import com.jetbrains.rider.test.annotations.report.Issues
import com.jetbrains.rider.test.junit5.base.PerTestProjectModelTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.facades.projectmodel.ProjectModelDumpApiFacade
import com.jetbrains.rider.test.facades.projectmodel.ProjectModelDumpApiFacade.CompareProjFileOptions.XmlNodes
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

@Tag(TeamCityTags.Plugins.FSharp.General)
@Solution("EmptySolution")
class FSharpProjectModelTest : PerTestProjectModelTestBase() {
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

  private fun ProjectModelDumpApiFacade.dumpWithSingleProjectMapping(caption: String, action: () -> Unit) {
    dumpAfterAction(caption, action)
    dump.treeOutput.append(project.fcsHost.dumpSingleProjectMapping.sync(Unit))
  }

  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("FSharpProjectTree")
  fun testFSharpProjectStructure() {
    withDump({ checkSlnFile = false; compareProjFilesOptions = XmlNodes() }) {
      dumpWithSingleProjectMapping("Init") {
      }
      dumpWithSingleProjectMapping(
        "1. Move file 'Folder(1)/File1.fs' inside other part of the same folder after 'Folder(2)/File4.fs'"
      ) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File1.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "File4.fs")
        )
      }
      dumpWithSingleProjectMapping(
        "2. Move file 'Folder(2)/File3.fs' inside other part of the same folder before 'Folder(1)/File2.fs'"
      ) {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "File3.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File2.fs"), RdDndOrderType.Before
        )
      }
      dumpWithSingleProjectMapping("3. Move file 'Folder(2)/File1.fs' before folder 'Folder(2)'") {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "File1.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2"), RdDndOrderType.Before
        )
      }
      dumpWithSingleProjectMapping("4. Move file 'File3.fs' and 'File1.fs' in folder 'Folder(2)/Sub(1)' before 'Class1.fs'") {
        moveItem(
          arrayOf(
            arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"),
            arrayOf("FSharpProjectTree", "ClassLibrary1", "File1.fs")
          ),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "Class1.fs"), RdDndOrderType.Before
        )
      }
      dumpWithSingleProjectMapping("5. Move 'Folder/Sub/File3.fs' to project folder before EmptyFolder") {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?1", "File3.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder"), RdDndOrderType.Before
        )
      }
      dumpWithSingleProjectMapping("6. Move 'Folder/Sub/File3.fs' to project folder after EmptyFolder") {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder"), RdDndOrderType.After
        )
      }
      dumpWithSingleProjectMapping("7. Move file 'Class2.fs' in folder 'Folder(2)' before 'Sub(2)'") {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?2", "Class2.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?2", "Sub?2"), RdDndOrderType.Before
        )
      }
      dumpWithSingleProjectMapping("8. Move file 'Folder(1)/File2.fs' before folder 'Folder(1)/File3.fs'") {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File2.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File3.fs"), RdDndOrderType.Before
        )
      }
      dumpWithSingleProjectMapping("9. Move file 'Folder/File2.fs' before 'Folder(1)'") {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1", "File2.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Folder?1"), RdDndOrderType.Before
        )
      }
      dumpWithSingleProjectMapping("10. Rename file 'File3.fs' to 'Foo.fs'") {
        renameItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "File3.fs"), "Foo.fs"
        )
      }
      dumpWithSingleProjectMapping("11. Move file 'Foo.fs' to 'EmptyFolder(1)'") {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "Foo.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder?1")
        )
      }
      dumpWithSingleProjectMapping("12. Move file 'EmptyFolder/Foo.fs' before 'EmptyFolder(1)'") {
        moveItem(
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder?1", "Foo.fs"),
          arrayOf("FSharpProjectTree", "ClassLibrary1", "EmptyFolder?1"), RdDndOrderType.Before
        )
      }
      dumpWithSingleProjectMapping("13. Move file 'File1.fs' and 'Class1.fs' in folder 'Folder(2)' before 'Sub(1)'") {
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
    withDump {
      dumpWithSingleProjectMapping("1. Create project") {
        addProject(project, arrayOf("Solution"), "ClassLibrary", ProjectTemplates.Sdk.Net9.FSharp.classLibrary, targetFramework = "netstandard2.1")
      }
      dumpWithSingleProjectMapping("2. Create folder 'NewFolder'") {
        addNewFolder(arrayOf("Solution", "ClassLibrary"), "NewFolder")
      }
      dumpWithSingleProjectMapping("3. Create subfolder 'NewFolder/NewSub'") {
        addNewFolder(arrayOf("Solution", "ClassLibrary", "NewFolder"), "NewSub")
      }
      dumpWithSingleProjectMapping("4. Move folder 'NewFolder/NewSub' to project root") {
        moveItem(
          arrayOf("Solution", "ClassLibrary", "NewFolder", "NewSub"),
          arrayOf("Solution", "ClassLibrary")
        )
      }
      dumpWithSingleProjectMapping("5. Delete folder 'NewSub'") {
        deleteElement(arrayOf("Solution", "ClassLibrary", "NewSub"))
      }
    }
  }

  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("FsprojWithTwoFiles")
  fun testManualFsprojChange() {
    withDump {
      dumpWithSingleProjectMapping("Init") { }

      dumpWithSingleProjectMapping("Move File1 and File2 lines") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }
    }
  }

  @Test
  @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
  @Solution("FSharpProjectTree")
  fun testFSharpMoveFolder() {
    withDump {
      dumpWithSingleProjectMapping("Init") { }

      // Move a whole folder part to a new position among top level items. Its items move
      // sequentially, like individual files.
      dumpWithSingleProjectMapping("1. Move folder 'Folder(1)' after 'File3.fs'") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }

      // Move a folder part next to another part of the same folder: the parts should join
      // into a single folder.
      dumpWithSingleProjectMapping("1. Move folder 'Folder(2)' before 'Folder(1)' (join parts)") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }

      // Move a folder part that contains a nested (split) subfolder 'Sub'. The nested structure
      // and item order must be preserved at the destination.
      dumpWithSingleProjectMapping("1. Move folder 'Folder(2)' (with nested 'Sub') before 'EmptyFolder'") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }
      dumpWithSingleProjectMapping("1. Move folder 'Folder' before 'File0.fs'") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }
      dumpWithSingleProjectMapping("1. Move empty folder 'EmptyFolder' before 'File3.fs'") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }
      dumpWithSingleProjectMapping("1. Move empty folder 'FolderA' after 'FolderB'") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }
      dumpWithSingleProjectMapping("1. Move folder 'Outer' after 'Sibling.fs'") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }
      dumpWithSingleProjectMapping("1. Move folder 'Folder1(2)' before 'Folder2'") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }
      dumpWithSingleProjectMapping("1. Move 'Folder1(2)/Nested' after 'Folder1(1)/Nested/File1.fs'") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }
      dumpWithSingleProjectMapping("1. Move 'Folder1(2)/Nested' after 'Folder1(1)/Nested/File1.fs'") {
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
    withDump {
      dumpWithSingleProjectMapping("Init") { }
      dumpWithSingleProjectMapping("1. Move 'Folder1(2)/Nested' after 'Folder1(1)/Nested/File1.fs'") {
        moveItem(
          arrayOf("SplitNestedTwoFolders", "ClassLibrary1", "Folder1?2", "Nested"),
          arrayOf("SplitNestedTwoFolders", "ClassLibrary1", "Folder1?1", "Nested", "File1.fs"),
          RdDndOrderType.After
        )
      }
    }
  }
}
