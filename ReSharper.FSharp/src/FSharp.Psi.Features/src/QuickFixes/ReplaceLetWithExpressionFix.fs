namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceLetWithExpressionFix(error: ExpectedExpressionAfterLetError) =
    inherit FSharpQuickFixBase()

    let letExpr = error.LetExpr

    let rec removeDanglingIn (node: ITreeNode) =
        if isNull node then () else

        let meaningfulSibling = node.GetNextMeaningfulSibling()
        if getTokenType meaningfulSibling == FSharpTokenType.IN then
            ModificationUtil.DeleteChildRange(node.NextSibling, meaningfulSibling)
        else
            removeDanglingIn node.Parent

    override x.Text =
        let tokenText = getLetTokenText letExpr.BindingKeyword
        sprintf "Replace '%s' with expression" tokenText

    override x.IsAvailable _ =
        isValid letExpr &&

        let bindings = letExpr.Bindings
        bindings.Count = 1 && isValid bindings.[0].Expression

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(letExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let expr = ModificationUtil.ReplaceChild(letExpr, letExpr.Bindings.[0].Expression.Copy())
        removeDanglingIn expr
