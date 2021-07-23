package model

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import java.io.File

@Suppress("unused")
object RdFantomasModel : Root() {

    private val rdFcsParsingOptions = structdef {
        field("lastSourceFile", string)
        field("lightSyntax", bool.nullable)
        field("conditionalCompilationDefines", array(string))
        field("isExe", bool)
    }

    private val rdFcsRange = structdef {
        field("fileName", string)
        field("startLine", int)
        field("startCol", int)
        field("endLine", int)
        field("endCol", int)
    }

    private val rdFantomasFormatConfig = structdef {
        field("indentSize", int)
        field("maxLineLength", int)
        field("spaceBeforeParameter", bool)
        field("spaceBeforeLowercaseInvocation", bool)
        field("spaceBeforeUppercaseInvocation", bool)
        field("spaceBeforeClassConstructor", bool)
        field("spaceBeforeMember", bool)
        field("spaceBeforeColon", bool)
        field("spaceAfterComma", bool)
        field("spaceBeforeSemicolon", bool)
        field("spaceAfterSemicolon", bool)
        field("indentOnTryWith", bool)
        field("spaceAroundDelimiter", bool)
        field("maxIfThenElseShortWidth", int)
        field("maxInfixOperatorExpression", int)
        field("maxRecordWidth", int)
        field("maxArrayOrListWidth", int)
        field("maxValueBindingWidth", int)
        field("maxFunctionBindingWidth", int)
        field("multilineBlockBracketsOnSameColumn", bool)
        field("newlineBetweenTypeDefinitionAndMembers", bool)
        field("keepIfThenInSameLine", bool)
        field("maxElmishWidth", int)
        field("singleArgumentWebMode", bool)
        field("alignFunctionSignatureToIndentation", bool)
        field("alternativeLongMemberDefinitions", bool)
        field("semicolonAtEndOfLine", bool)
        field("multiLineLambdaClosingNewline", bool)
        field("maxRecordNumberOfItems", int)
        field("maxArrayOrListNumberOfItems", int)
        field("maxDotGetExpressionWidth", int)
        field("disableElmishSyntax", bool)
        field("keepIndentInBranch", bool)
        field("blankLinesAroundNestedMultilineExpressions", bool)
        field("barBeforeDiscriminatedUnionDeclaration", bool)
        field("strictMode", bool)
        field("recordMultilineFormatter", string)
        field("arrayOrListMultilineFormatter", string)
    }

    private val rdFormatArgs = basestruct {
        field("fileName", string)
        field("source", string)
        field("formatConfig", rdFantomasFormatConfig)
        field("parsingOptions", rdFcsParsingOptions)
        field("newLineText", string)
    }

    init {
        call("formatDocument", structdef("rdFormatDocumentArgs") extends rdFormatArgs {}, string)
        call("formatSelection", structdef("rdFormatSelectionArgs") extends rdFormatArgs {
            field("range", rdFcsRange)
        }, string)

        signal("exit", void)
    }
}
