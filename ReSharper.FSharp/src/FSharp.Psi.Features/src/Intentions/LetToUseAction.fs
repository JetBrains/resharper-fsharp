namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

module LetToUseAction =
    let [<Literal>] Description = "Convert to 'use' binding"

[<ContextAction(Name = "LetToUse", Group = "F#", Description = ToLiteralAction.Description)>]
type LetToUseAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override x.Text = "To 'use'"

    override x.IsAvailable _ =
        let letExpr = dataProvider.GetSelectedElement<ILetOrUseExpr>()
        if not (isAtLetExprKeywordOrNamedPat dataProvider letExpr) then false else
        LetDisposableAnalyzer.isApplicable letExpr

    override x.ExecutePsiTransaction(_, _) =
        let letExpr = dataProvider.GetSelectedElement<ILetOrUseExpr>()
        LetToUseAction.Execute(letExpr)
        null

    static member Execute(letExpr: ILetOrUseExpr) =
        use writeCookie = WriteLockCookie.Create(letExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let tokenType = getTokenType letExpr.BindingKeyword
        let tokenType = if tokenType == FSharpTokenType.LET_BANG then FSharpTokenType.USE_BANG else FSharpTokenType.USE

        ModificationUtil.ReplaceChild(letExpr.BindingKeyword, tokenType.CreateLeafElement()) |> ignore
