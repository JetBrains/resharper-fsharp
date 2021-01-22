package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.workspaceModel.ide.impl.virtualFile
import com.jetbrains.rider.model.RdProjectFileDescriptor
import com.jetbrains.rider.model.RdProjectFolderDescriptor
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.containingProjectEntity

fun ProjectModelEntity.isFromFSharpProject(): Boolean =
        containingProjectEntity()?.url?.virtualFile?.extension.equals("fsproj", true)

fun ProjectModelEntity.getSortKey(): Int =
        when (val descriptor = this.descriptor) {
            is RdProjectFileDescriptor -> descriptor.sortKey ?: -1
            is RdProjectFolderDescriptor -> descriptor.sortKey ?: -1
            else -> -1
        }
