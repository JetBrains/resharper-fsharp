namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithRegularStringFix(warning: RedundantStringInterpolationWarning) =
    inherit FSharpQuickFixBase()

    let expr = warning.Expr

    override this.IsAvailable _ =
        isValid expr &&
        expr.Literals.Count = 1 &&
        expr.Literals.Single().GetTokenType() |> isFullInterpolatedStringToken

    override this.Text = "Replace with regular string"

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let interpolatedStringToken = expr.Literals.Single()
        let text = interpolatedStringToken.GetText()

        let regularStringType, fixedText =
            match interpolatedStringToken.GetTokenType() with
            | a when a == FSharpTokenType.REGULAR_INTERPOLATED_STRING -> FSharpTokenType.STRING, text.Substring(1)
            | a when a == FSharpTokenType.VERBATIM_INTERPOLATED_STRING -> FSharpTokenType.VERBATIM_STRING, $"@{text.Substring(2)}"
            | a when a == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING -> FSharpTokenType.TRIPLE_QUOTED_STRING, text.Substring(1)
            | a -> failwith $"Unexpected token type: {a}"

        let regularString = FSharpString(regularStringType, fixedText)
        let stringExpr = ElementType.LITERAL_EXPR.Create()
        stringExpr.AppendNewChild regularString

        replace expr stringExpr
