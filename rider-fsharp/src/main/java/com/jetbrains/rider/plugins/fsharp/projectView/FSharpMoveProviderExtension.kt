package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.openapi.project.Project
import com.intellij.workspaceModel.ide.impl.virtualFile
import com.jetbrains.rider.model.RdProjectFileDescriptor
import com.jetbrains.rider.projectView.ProjectElementView
import com.jetbrains.rider.projectView.ProjectEntityView
import com.jetbrains.rider.projectView.moveProviders.extensions.MoveProviderExtension
import com.jetbrains.rider.projectView.moveProviders.impl.ActionOrderType
import com.jetbrains.rider.projectView.moveProviders.impl.NodeOrderType
import com.jetbrains.rider.projectView.nodes.*
import com.jetbrains.rider.projectView.utils.compareProjectModelEntities
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.containingProjectEntity
import com.jetbrains.rider.projectView.workspace.isProjectFile
import com.jetbrains.rider.projectView.workspace.isProjectFolder
import com.jetbrains.rider.util.idea.application

class FSharpMoveProviderExtension(project: Project) : MoveProviderExtension(project) {

    companion object {
        const val CompileBeforeType: String = "CompileBefore"
        const val CompileAfterType: String = "CompileAfter"

        fun isSpecialCompileType(descriptor: RdProjectFileDescriptor) : Boolean {
            return descriptor.buildAction in arrayOf(CompileBeforeType, CompileAfterType)
        }
    }

    private fun ProjectModelEntity.prevSibling() = getSibling { index -> index - 1 }
    private fun ProjectModelEntity.nextSibling() = getSibling { index -> index + 1 }

    private fun ProjectModelEntity.getSibling(indexFunc: (Int) -> Int): ProjectModelEntity? {
        val parent = parentEntity ?: return null
        val siblings = parent.getSortedChildren()
        val index = siblings.indexOf(this)
        val newIndex = indexFunc(index)
        if (newIndex < 0 || newIndex > siblings.count() - 1)
            return null
        return siblings[newIndex]
    }

    private fun ProjectModelEntity.getSortedChildren(): List<ProjectModelEntity> {
        val comparator = Comparator<ProjectModelEntity> { p0, p1 ->
            if (p0 == null && p1 == null) return@Comparator 0
            if (p0 == null) return@Comparator 1
            if (p1 == null) return@Comparator -1
            compareProjectModelEntities(project, p0, p1)
        }
        return childrenEntities.sortedWith(comparator).toList()
    }

    override fun supportOrdering(element: ProjectElementView): NodeOrderType {
        if (element is ProjectEntityView && isFSharpNode(element.entity)) {
            if (element.entity.isProjectFile()) return NodeOrderType.BeforeAfter
            if (element.entity.isProjectFolder()) return NodeOrderType.BeforeAfterInside
            return NodeOrderType.None
        }
        return super.supportOrdering(element)
    }

    override fun allowPaste(entities: Collection<ProjectModelEntity>, relativeTo: ProjectElementView, orderType: ActionOrderType): Boolean {
        if (entities.any { it.isProjectFolder() && it.containingProjectEntity()?.url?.virtualFile?.extension == "fsproj" })
            return false

        if (orderType == ActionOrderType.None) {
            return super.allowPaste(entities, relativeTo, orderType)
        }

        if (relativeTo is ProjectEntityView && isFSharpNode(relativeTo.entity)) {
            val nodesItemType = getNodesItemType(entities)
            if (nodesItemType == FSharpItemType.Mix) return false

            val relativeToEntity = relativeTo.entity
            when (orderType) {
                ActionOrderType.Before -> {
                    when (nodesItemType) {
                        FSharpItemType.Default -> {
                            if (relativeToEntity.isCompileBefore(ActionOrderType.Before))
                                return false
                            if (relativeToEntity.prevSibling().isCompileAfter(ActionOrderType.After))
                                return false
                            return true
                        }
                        FSharpItemType.CompileBefore ->
                            return relativeToEntity.isCompileBefore(ActionOrderType.Before) ||
                                    relativeToEntity.prevSibling().isCompileBefore(ActionOrderType.After)
                        FSharpItemType.CompileAfter -> return relativeToEntity.isCompileAfter(ActionOrderType.Before)
                        else -> {
                        }
                    }
                }
                ActionOrderType.After -> {
                    when (nodesItemType) {
                        FSharpItemType.Default -> {
                            if (relativeToEntity.isCompileAfter(ActionOrderType.After))
                                return false
                            if (relativeToEntity.nextSibling().isCompileBefore(ActionOrderType.Before))
                                return false
                            return true
                        }
                        FSharpItemType.CompileBefore -> return relativeToEntity.isCompileBefore(ActionOrderType.After)
                        FSharpItemType.CompileAfter ->
                            return relativeToEntity.isCompileAfter(ActionOrderType.After) ||
                                    relativeToEntity.nextSibling().isCompileAfter(ActionOrderType.Before)
                        else -> {
                        }
                    }
                }
                ActionOrderType.None -> throw Exception()
            }

        }
        return super.allowPaste(entities, relativeTo, orderType)
    }

    private fun getNodesItemType(entities: Collection<ProjectModelEntity>): FSharpItemType {
        var compileBeforeFound = false
        var compileAfterFound = false
        var other = false
        for (node in entities) {
            if (node.isProjectFile()) {
                when {
                    node.isCompileBefore(ActionOrderType.None) -> compileBeforeFound = true
                    node.isCompileAfter(ActionOrderType.None) -> compileAfterFound = true
                    else -> other = true
                }
            }
            if (node.isProjectFolder()) {
                when (getNodesItemType(node.childrenEntities.toList())) {
                    FSharpItemType.Default -> {
                        other = true
                    }
                    FSharpItemType.Mix -> {
                        compileBeforeFound = true
                        compileAfterFound = true
                    }
                    FSharpItemType.CompileBefore -> {
                        compileBeforeFound = true
                    }
                    FSharpItemType.CompileAfter -> {
                        compileAfterFound = true
                    }
                }
            }
        }

        if (!compileBeforeFound && !compileAfterFound) return FSharpItemType.Default
        if (compileBeforeFound && !compileAfterFound && !other) return FSharpItemType.CompileBefore
        if (!compileBeforeFound && compileAfterFound && !other) return FSharpItemType.CompileAfter
        return FSharpItemType.Mix
    }

    private fun isFSharpNode(entity: ProjectModelEntity): Boolean {
        return entity.containingProjectEntity()?.url?.virtualFile?.extension == "fsproj" ||
                application.isUnitTestMode // todo: workaround for dummy project?
    }

    private fun ProjectModelEntity?.isCompileBefore(orderType: ActionOrderType): Boolean {
        this ?: return false
        val descriptor = descriptor
        if (descriptor is RdProjectFileDescriptor) {
            return descriptor.buildAction == CompileBeforeType
        }
        if (isProjectFolder()) {
            return when (orderType) {
                ActionOrderType.Before -> getSortedChildren().firstOrNull()?.isCompileBefore(orderType) == true
                ActionOrderType.After -> getSortedChildren().lastOrNull()?.isCompileBefore(orderType) == true
                ActionOrderType.None -> false
            }
        }
        return false
    }

    private fun ProjectModelEntity?.isCompileAfter(orderType: ActionOrderType): Boolean {
        this ?: return false
        val descriptor = descriptor
        if (descriptor is RdProjectFileDescriptor) {
            return descriptor.buildAction == CompileAfterType
        }
        if (isProjectFolder()) {
            return when (orderType) {
                ActionOrderType.Before -> getSortedChildren().firstOrNull()?.isCompileAfter(orderType) == true
                ActionOrderType.After -> getSortedChildren().lastOrNull()?.isCompileAfter(orderType) == true
                ActionOrderType.None -> false
            }
        }
        return false
    }

    enum class FSharpItemType {
        Default,
        Mix,
        CompileBefore,
        CompileAfter
    }
}