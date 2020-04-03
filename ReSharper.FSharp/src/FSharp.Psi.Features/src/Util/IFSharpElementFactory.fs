namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util

open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Modules

type IFSharpElementFactory =
    abstract CreateOpenStatement: ns: string -> IOpenStatement
    abstract CreateWildPat: unit -> IWildPat

    abstract CreateParenExpr: unit -> IParenExpr
    abstract CreateParenExpr: ISynExpr -> IParenExpr

    abstract CreateConstExpr: text: string -> IConstExpr

    abstract CreateAppExpr: addSpace: bool -> IPrefixAppExpr
    abstract CreateAppExpr: funcName: string * arg: ISynExpr -> IPrefixAppExpr
    abstract CreateAppExpr: funExpr: ISynExpr * argExpr: ISynExpr * addSpace: bool -> IPrefixAppExpr
    abstract CreateBinaryAppExpr: string * left: ISynExpr * right: ISynExpr -> ISynExpr
    abstract CreateSetExpr: left: ISynExpr * right: ISynExpr -> ISynExpr

    abstract CreateExpr: string -> ISynExpr
    abstract CreateReferenceExpr: string -> ISynExpr
    
    abstract CreateLetBindingExpr: bindingName: string -> ILetOrUseExpr
    abstract CreateLetBindingExpr: bindingName: string * expr: ISynExpr -> ILetOrUseExpr

    abstract CreateLetModuleDecl: bindingName: string * expr: ISynExpr -> ILetModuleDecl

    abstract CreateIgnoreApp: ISynExpr * newLine: bool -> IBinaryAppExpr
    abstract CreateRecordExprBinding: fieldName: string * addSemicolon: bool -> IRecordExprBinding

    abstract CreateMatchExpr: ISynExpr -> IMatchExpr
    abstract CreateForEachExpr: ISynExpr -> IForEachExpr

    abstract AsReferenceExpr: typeReference: ITypeReferenceName -> IReferenceExpr

[<AllowNullLiteral>]
type IFSharpLanguageService =
    abstract CreateParser: IDocument -> IFSharpParser
    abstract CreateElementFactory: IPsiModule -> IFSharpElementFactory
