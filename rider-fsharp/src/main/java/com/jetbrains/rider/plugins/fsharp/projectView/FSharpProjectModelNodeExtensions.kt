package com.jetbrains.rider.plugins.fsharp.projectView

import com.jetbrains.rider.model.RdProjectFileDescriptor
import com.jetbrains.rider.model.RdProjectFolderDescriptor
import com.jetbrains.rider.projectView.nodes.ProjectModelNode
import com.jetbrains.rider.projectView.nodes.containingProject

fun ProjectModelNode.isFromFSharpProject(): Boolean =
        this.containingProject()?.getVirtualFile()?.extension == "fsproj"

fun ProjectModelNode.getSortKey(): Int =
        when (val descriptor = this.descriptor) {
            is RdProjectFileDescriptor -> descriptor.sortKey ?: -1
            is RdProjectFolderDescriptor -> descriptor.sortKey ?: -1
            else -> -1
        }
