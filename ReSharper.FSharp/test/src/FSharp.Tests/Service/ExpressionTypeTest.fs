namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open NUnit.Framework

[<FSharpTest>]
type ExpressionTypeTest() =
    inherit TopBindingExpressionTestBase()

    let presentType (expr: IExpression) =
        expr.Type().GetLongPresentableName(FSharpLanguage.Instance)

    override x.RelativeTestDataPath = "features/service/expressionType"

    override x.Present(expr) =
        match expr with
        | :? ILiteralExpr as literalExpr ->
            match literalExpr.Literal with
            | :? IExpression as literal -> $"{presentType expr}, token: {presentType literal}"
            | _ -> presentType expr
        | _ -> presentType expr

    [<Test>] member x.``Literals 01``() = x.DoNamedTest()
