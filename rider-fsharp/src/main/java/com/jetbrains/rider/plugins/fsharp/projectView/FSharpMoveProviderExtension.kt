package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectView.moveProviders.Impl.ActionOrderType
import com.jetbrains.rider.projectView.moveProviders.Impl.NodeOrderType
import com.jetbrains.rider.projectView.moveProviders.extensions.MoveProviderExtension
import com.jetbrains.rider.projectView.nodes.*
import com.jetbrains.rider.util.idea.application

class FSharpMoveProviderExtension(project: Project) : MoveProviderExtension(project) {

    companion object {
        const val FSharpCompileType: String = "FSharpCompileType"
        const val CompileBeforeType: String = "CompileBefore"
        const val CompileAfterType: String = "CompileAfter"
    }

    override fun supportOrdering(node: IProjectModelNode): NodeOrderType {
        if (node is ProjectModelNode && isFSharpNode(node)) {
            if (node.isProjectFile()) return NodeOrderType.BeforeAfter
            if (node.isProjectFolder()) return NodeOrderType.BeforeAfterInside
            return NodeOrderType.None
        }
        return super.supportOrdering(node)
    }

    override fun allowPaste(nodes: Collection<ProjectModelNode>, relativeTo: IProjectModelNode, orderType: ActionOrderType): Boolean {
        if (nodes.any { it.isProjectFolder() && it.containingProject()?.getVirtualFile()?.extension == "fsproj" })
            return false

        if (orderType == ActionOrderType.None) {
            return super.allowPaste(nodes, relativeTo, orderType)
        }

        if (relativeTo is ProjectModelNode && isFSharpNode(relativeTo)) {
            val nodesItemType = getNodesItemType(nodes)
            if (nodesItemType == FSharpItemType.Mix) return false

            when (orderType) {
                ActionOrderType.Before -> {
                    when (nodesItemType) {
                        FSharpItemType.Default -> {
                            if (relativeTo.isCompileBefore(ActionOrderType.Before))
                                return false
                            if (relativeTo.prevSibling().isCompileAfter(ActionOrderType.After))
                                return false
                            return true
                        }
                        FSharpItemType.CompileBefore ->
                            return relativeTo.isCompileBefore(ActionOrderType.Before) ||
                                    relativeTo.prevSibling().isCompileBefore(ActionOrderType.After)
                        FSharpItemType.CompileAfter -> return relativeTo.isCompileAfter(ActionOrderType.Before)
                        else -> {
                        }
                    }
                }
                ActionOrderType.After -> {
                    when (nodesItemType) {
                        FSharpItemType.Default -> {
                            if (relativeTo.isCompileAfter(ActionOrderType.After))
                                return false
                            if (relativeTo.nextSibling().isCompileBefore(ActionOrderType.Before))
                                return false
                            return true
                        }
                        FSharpItemType.CompileBefore -> return relativeTo.isCompileBefore(ActionOrderType.After)
                        FSharpItemType.CompileAfter ->
                            return relativeTo.isCompileAfter(ActionOrderType.After) ||
                                    relativeTo.nextSibling().isCompileAfter(ActionOrderType.Before)
                        else -> {
                        }
                    }
                }
                ActionOrderType.None -> throw Exception()
            }

        }
        return super.allowPaste(nodes, relativeTo, orderType)
    }

    private fun getNodesItemType(nodes: Collection<ProjectModelNode>): FSharpItemType {
        var compileBeforeFound = false
        var compileAfterFound = false
        var other = false
        for (node in nodes) {
            if (node.isProjectFile()) {
                when {
                    node.isCompileBefore(ActionOrderType.None) -> compileBeforeFound = true
                    node.isCompileAfter(ActionOrderType.None) -> compileAfterFound = true
                    else -> other = true
                }
            }
            if (node.isProjectFolder()) {
                when (getNodesItemType(node.getChildren(true, false))) {
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

    private fun isFSharpNode(node: ProjectModelNode): Boolean {
        return node.containingProject()?.getVirtualFile()?.extension == "fsproj" ||
                application.isUnitTestMode // todo: workaround for dummy project?
    }

    private fun ProjectModelNode?.isCompileBefore(orderType: ActionOrderType): Boolean {
        this ?: return false
        if (isProjectFile()) {
            return descriptor.getUserData(FSharpCompileType) == CompileBeforeType
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

    private fun ProjectModelNode?.isCompileAfter(orderType: ActionOrderType): Boolean {
        this ?: return false
        if (isProjectFile()) {
            return descriptor.getUserData(FSharpCompileType) == CompileAfterType
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