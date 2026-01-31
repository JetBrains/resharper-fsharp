package com.jetbrains.rider.plugins.fsharp.fileStructure

import com.intellij.ide.util.treeView.smartTree.Sorter
import com.jetbrains.rider.fileStructure.RiderExtendedFileStructureBase

class FSharpExtendedFileStructureSupport : RiderExtendedFileStructureBase() {
  override val sorters: Array<Sorter> get() = emptyArray()
}
