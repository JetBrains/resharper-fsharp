package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.injections

import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveState
import com.intellij.psi.impl.source.resolve.FileContextUtil
import com.intellij.psi.util.elementType
import com.intellij.sql.psi.*
import com.intellij.sql.psi.impl.SqlReferenceImpl
import com.intellij.sql.psi.impl.SqlResolveExtension
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.plugins.appender.database.FakeDasSymbol

class FSharpSqlDapperParameterResolveExtension : SqlResolveExtension {
  override fun process(reference: SqlReferenceImpl, processor: SqlScopeProcessor) {
    if (!processor.isResultEmpty) return
    val element = reference.element
    val containingFile = element.containingFile.getUserData(FileContextUtil.INJECTED_IN_ELEMENT)?.containingFile ?: return
    if (element.elementType != SqlCompositeElementTypes.SQL_VARIABLE_REFERENCE) return
    if (!(element.isBinaryExpression or element.isFunctionCallArgument)) return
    if (!containingFile.language.`is`(FSharpLanguage)) return
    processor.executeTarget(FakeDasSymbol(reference.element), null, null, ResolveState.initial())
  }

  private val PsiElement.isBinaryExpression: Boolean
    get() = this.parent is SqlBinaryExpression

  private val PsiElement.isFunctionCallArgument: Boolean
    get() = this.parent is SqlExpressionList && this.parent.parent is SqlFunctionCallExpression
}