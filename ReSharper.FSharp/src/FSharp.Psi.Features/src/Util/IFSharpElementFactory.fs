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
    abstract CreateIgnoreApp: ISynExpr -> IAppExpr
    abstract CreateRecordExprBinding: fieldName: string * addSemicolon: bool -> IRecordExprBinding


[<AllowNullLiteral>]
type IFSharpLanguageService =
    abstract CreateParser: IDocument -> IFSharpParser
    abstract CreateElementFactory: IPsiModule -> IFSharpElementFactory


[<Language(typeof<FSharpLanguage>)>]
type FSharpExpressionProvider() =
    inherit ExpressionSelectionProviderBase<IFSharpTreeNode>()

    override x.IsTokenSkipped _ = false
