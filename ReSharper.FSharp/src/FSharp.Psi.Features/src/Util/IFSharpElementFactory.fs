namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util

open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Modules

type IFSharpElementFactory =
    abstract CreateOpenStatement: ns: string -> IOpenStatement
    abstract CreateWildPat: unit -> IWildPat

    abstract CreateParenExpr: unit -> IParenExpr
    abstract CreateParenExpr: IFSharpExpression -> IParenExpr

    abstract CreateConstExpr: text: string -> IConstExpr

    abstract CreateAppExpr: addSpace: bool -> IPrefixAppExpr
    abstract CreateAppExpr: funcName: string * arg: IFSharpExpression -> IPrefixAppExpr
    abstract CreateAppExpr: funExpr: IFSharpExpression * argExpr: IFSharpExpression * addSpace: bool -> IPrefixAppExpr
    abstract CreateBinaryAppExpr: string * left: IFSharpExpression * right: IFSharpExpression -> IFSharpExpression
    abstract CreateSetExpr: left: IFSharpExpression * right: IFSharpExpression -> IFSharpExpression

    abstract CreateExpr: string -> IFSharpExpression
    abstract CreateReferenceExpr: string -> IFSharpExpression
    
    abstract CreateLetBindingExpr: bindingName: string -> ILetOrUseExpr
    abstract CreateLetModuleDecl: bindingName: string -> ILetModuleDecl

    abstract CreateIgnoreApp: IFSharpExpression * newLine: bool -> IBinaryAppExpr
    abstract CreateRecordExprBinding: fieldName: string * addSemicolon: bool -> IRecordExprBinding

    abstract CreateParenPat: unit -> IParenPat
    abstract CreateTypedPat: pattern: string * typeUsage: ITypeUsage * spaceBeforeColon: bool -> ITypedPat
    
    abstract CreateTypeUsage: typeUsage: string -> ITypeUsage
    abstract CreateTypeUsage: typeUsages: ITypeUsage list -> ITypeUsage
    
    abstract CreateReturnTypeInfo: typeSignature: ITypeUsage -> IReturnTypeInfo
    
    abstract CreateMatchExpr: IFSharpExpression -> IMatchExpr
    abstract CreateMatchClause: unit -> IMatchClause
    abstract CreateForEachExpr: IFSharpExpression -> IForEachExpr

    abstract AsReferenceExpr: typeReference: ITypeReferenceName -> IReferenceExpr

    abstract CreateEmptyAttributeList: unit -> IAttributeList
    abstract CreateAttribute: attrName: string -> IAttribute


[<AllowNullLiteral>]
type IFSharpLanguageService =
    abstract CreateParser: IDocument -> IFSharpParser
    abstract CreateElementFactory: IPsiModule -> IFSharpElementFactory
