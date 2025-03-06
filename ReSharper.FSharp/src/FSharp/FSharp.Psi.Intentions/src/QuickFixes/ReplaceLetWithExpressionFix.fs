namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

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

    override x.Text = $"Replace '{getLetTokenText letExpr.BindingKeyword}' with expression"

    override x.IsAvailable _ =
        isValid letExpr &&

        let bindings = letExpr.Bindings
        bindings.Count = 1 &&

        let binding = bindings[0]
        isValid binding.Expression && binding.ParametersDeclarationsEnumerable.IsEmpty()

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(letExpr.IsPhysical())

        let expr = ModificationUtil.ReplaceChild(letExpr, letExpr.Bindings[0].Expression.Copy())
        removeDanglingIn expr
