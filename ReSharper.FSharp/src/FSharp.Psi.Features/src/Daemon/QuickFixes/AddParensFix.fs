namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type AddParensFix(error: SuccessiveArgsShouldBeSpacedOrTupledError) =
    inherit QuickFixBase()

    let expr = error.Expr

    override x.Text = "Add parens"
    override x.IsAvailable _ = isValid expr

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let exprCopy = expr.Copy()
        let factory = expr.CreateElementFactory()

        let parenExpr = factory.CreateParenExpr()
        let parenExpr = ModificationUtil.ReplaceChild(expr, parenExpr)
        let expr = parenExpr.SetInnerExpression(exprCopy)

        if not expr.IsSingleLine then
            for child in List.ofSeq (expr.Tokens()) do
                if not (child :? NewLine) then () else

                let nextSibling = child.NextSibling
                if nextSibling :? NewLine then () else
                if not (expr.Contains(nextSibling)) then () else

                if nextSibling :? Whitespace then
                    if nextSibling.NextSibling.IsWhitespaceToken() then () else
                        let length = nextSibling.GetTextLength() + 1
                        ModificationUtil.ReplaceChild(nextSibling, Whitespace(length)) |> ignore
                else
                    ModificationUtil.AddChildAfter(child, Whitespace()) |> ignore
        
        null
