namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Text.RegularExpressions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type ConvertTupleToArrayOrListElementsFix(warning: AddTypeEquationError) =
    inherit FSharpQuickFixBase()

    let regex = Regex("'\w\d*( \* '\w\d*)+")
    let expr = warning.Expr
    let actualType = warning.ActualType
    let arrayOrListExpr = ArrayOrListExprNavigator.GetByExpression(expr.IgnoreParentParens())

    override x.Text = "Use ';' separators"

    override x.IsAvailable _ =
        isValid expr && isNotNull arrayOrListExpr && regex.Match(actualType).Value = actualType

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        match expr with
        | :? ITupleExpr as tuple ->
            for comma in tuple.Commas do
                replaceWithToken comma FSharpTokenType.SEMICOLON

            let seqExpr = ModificationUtil.ReplaceChild(tuple.IgnoreParentParens(), ElementType.SEQUENTIAL_EXPR.Create())
            LowLevelModificationUtil.AddChild(seqExpr, tuple.Children().AsArray())
        | _ -> ()
