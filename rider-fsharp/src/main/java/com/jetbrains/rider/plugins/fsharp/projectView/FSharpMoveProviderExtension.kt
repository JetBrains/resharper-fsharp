package com.jetbrains.rider.plugins.fsharp.projectView

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.intellij.platform.backend.workspace.virtualFile
import com.jetbrains.rd.ide.model.RdDndOrderType
import com.jetbrains.rd.ide.model.RdDndTargetType
import com.jetbrains.rider.model.RdProjectFileDescriptor
import com.jetbrains.rider.model.RdProjectModelItemDescriptor
import com.jetbrains.rider.projectView.ProjectElementView
import com.jetbrains.rider.projectView.ProjectEntityView
import com.jetbrains.rider.projectView.moveProviders.extensions.MoveProviderExtension
import com.jetbrains.rider.projectView.nodes.getUserData
import com.jetbrains.rider.projectView.utils.compareProjectModelEntities
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.containingProjectEntity
import com.jetbrains.rider.projectView.workspace.isProjectFile
import com.jetbrains.rider.projectView.workspace.isProjectFolder
import com.jetbrains.rider.util.idea.application

@NlsSafe
fun getSpecialCompileType(descriptor: RdProjectModelItemDescriptor): String? {
  if (descriptor is RdProjectFileDescriptor) {
    return descriptor.getUserData("CompileOrder")
  }

  return null
}

class FSharpMoveProviderExtension(project: Project) : MoveProviderExtension(project) {

  companion object {
    const val CompileBeforeType: String = "CompileBefore"
    const val CompileAfterType: String = "CompileAfter"
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

  override fun supportOrdering(element: ProjectElementView): RdDndTargetType {
    if (element is ProjectEntityView && isFSharpNode(element.entity)) {
      if (element.entity.isProjectFile()) return RdDndTargetType.BeforeAfter
      if (element.entity.isProjectFolder()) return RdDndTargetType.BeforeAfterInside
      return RdDndTargetType.Default
    }
    return super.supportOrdering(element)
  }

  override fun allowPaste(
    entities: Collection<ProjectModelEntity>,
    relativeTo: ProjectElementView,
    orderType: RdDndOrderType
  ): Boolean {
    if (entities.any { it.isProjectFolder() && it.containingProjectEntity()?.url?.virtualFile?.extension == "fsproj" })
      return false

    if (orderType == RdDndOrderType.None) {
      return super.allowPaste(entities, relativeTo, orderType)
    }

    if (relativeTo is ProjectEntityView && isFSharpNode(relativeTo.entity)) {
      val nodesItemType = getNodesItemType(entities)
      if (nodesItemType == FSharpItemType.Mix) return false

      val relativeToEntity = relativeTo.entity
      when (orderType) {
        RdDndOrderType.Before -> {
          when (nodesItemType) {
            FSharpItemType.Default -> {
              if (relativeToEntity.isCompileBefore(RdDndOrderType.Before))
                return false
              if (relativeToEntity.prevSibling().isCompileAfter(RdDndOrderType.After))
                return false
              return true
            }

            FSharpItemType.CompileBefore ->
              return relativeToEntity.isCompileBefore(RdDndOrderType.Before) ||
                relativeToEntity.prevSibling().isCompileBefore(RdDndOrderType.After)

            FSharpItemType.CompileAfter -> return relativeToEntity.isCompileAfter(RdDndOrderType.Before)
            else -> {
            }
          }
        }

        RdDndOrderType.After -> {
          when (nodesItemType) {
            FSharpItemType.Default -> {
              if (relativeToEntity.isCompileAfter(RdDndOrderType.After))
                return false
              if (relativeToEntity.nextSibling().isCompileBefore(RdDndOrderType.Before))
                return false
              return true
            }

            FSharpItemType.CompileBefore -> return relativeToEntity.isCompileBefore(RdDndOrderType.After)
            FSharpItemType.CompileAfter ->
              return relativeToEntity.isCompileAfter(RdDndOrderType.After) ||
                relativeToEntity.nextSibling().isCompileAfter(RdDndOrderType.Before)

            else -> {
            }
          }
        }

        RdDndOrderType.None -> throw Exception()
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
          node.isCompileBefore(RdDndOrderType.None) -> compileBeforeFound = true
          node.isCompileAfter(RdDndOrderType.None) -> compileAfterFound = true
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

  private fun ProjectModelEntity?.isCompileBefore(orderType: RdDndOrderType): Boolean {
    this ?: return false
    val compileType = getSpecialCompileType(descriptor)
    if (compileType != null) {
      return compileType == CompileBeforeType
    }
    if (isProjectFolder()) {
      return when (orderType) {
        RdDndOrderType.Before -> getSortedChildren().firstOrNull()?.isCompileBefore(orderType) == true
        RdDndOrderType.After -> getSortedChildren().lastOrNull()?.isCompileBefore(orderType) == true
        RdDndOrderType.None -> false
      }
    }
    return false
  }

  private fun ProjectModelEntity?.isCompileAfter(orderType: RdDndOrderType): Boolean {
    this ?: return false
    val compileType = getSpecialCompileType(descriptor)
    if (compileType != null) {
      return compileType == CompileAfterType
    }
    if (isProjectFolder()) {
      return when (orderType) {
        RdDndOrderType.Before -> getSortedChildren().firstOrNull()?.isCompileAfter(orderType) == true
        RdDndOrderType.After -> getSortedChildren().lastOrNull()?.isCompileAfter(orderType) == true
        RdDndOrderType.None -> false
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
