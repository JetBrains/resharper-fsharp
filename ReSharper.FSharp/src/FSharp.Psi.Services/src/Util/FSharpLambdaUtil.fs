module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpLambdaUtil

open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

let deletePatternsFromEnd (lambda: ILambdaExpr) count =
    let paramGroupDecls = lambda.ParametersDeclarations
    Assertion.Assert(count > 0 && count < paramGroupDecls.Count, "count > 0 && count < pats.Count")

    let firstNodeToDelete = getFirstMatchingNodeBefore isWhitespace paramGroupDecls[paramGroupDecls.Count - count]
    let lastNodeToDelete = paramGroupDecls.Last()
    deleteChildRange firstNodeToDelete lastNodeToDelete

    let arrow = lambda.RArrow
    let nodeAfterPats = lambda.ParametersDeclarations.Last().NextSibling

    if nodeAfterPats != arrow then
        replaceRangeWithNode nodeAfterPats arrow.PrevSibling (Whitespace())
    else
        ModificationUtil.AddChildBefore(arrow, Whitespace()) |> ignore
