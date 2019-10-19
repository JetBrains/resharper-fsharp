namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util

open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.ExpressionSelection
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules

type IFSharpElementFactory =
    abstract CreateOpenStatement: ns: string -> IOpenStatement
    abstract CreateWildPat: unit -> IWildPat
    abstract CreateParenExpr: unit -> IParenExpr
    abstract CreateConstExpr: text: string -> IConstExpr

    abstract CreateAppExpr: addSpace: bool -> IAppExpr
    abstract CreateAppExpr: funcName: string * arg: ISynExpr -> IAppExpr
    abstract CreateAppExpr: funExpr: ISynExpr * argExpr: ISynExpr * addSpace: bool -> IAppExpr

    abstract CreateReferenceExpr: string -> ISynExpr
    
    abstract CreateLetBindingExpr: bindingName: string * expr: ISynExpr -> ILetOrUseExpr
    abstract CreateIgnoreApp: ISynExpr * newLine: bool -> IAppExpr
    abstract CreateRecordExprBinding: fieldName: string * addSemicolon: bool -> IRecordExprBinding
    abstract CreateMatchExpr: ISynExpr -> IMatchExpr

    abstract AsReferenceExpr: typeReference: ITypeReferenceName -> IReferenceExpr

[<AllowNullLiteral>]
type IFSharpLanguageService =
    abstract CreateParser: IDocument -> IFSharpParser
    abstract CreateElementFactory: IPsiModule -> IFSharpElementFactory
