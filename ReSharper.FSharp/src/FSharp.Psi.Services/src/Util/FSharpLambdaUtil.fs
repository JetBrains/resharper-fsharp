module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpLambdaUtil

open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

let deletePatternsFromEnd (lambda: ILambdaExpr) count =
    let paramGroups = lambda.PatternParameterGroups
    Assertion.Assert(count > 0 && count < paramGroups.Count, "count > 0 && count < pats.Count")

    let firstNodeToDelete = getFirstMatchingNodeBefore isWhitespace paramGroups[paramGroups.Count - count]
    let lastNodeToDelete = paramGroups.Last()
    deleteChildRange firstNodeToDelete lastNodeToDelete

    let arrow = lambda.RArrow
    let nodeAfterPats = lambda.PatternParameterGroupsEnumerable.LastOrDefault().NextSibling // todo

    if nodeAfterPats != arrow then replaceRangeWithNode nodeAfterPats arrow.PrevSibling (Whitespace())
    else ModificationUtil.AddChildBefore(arrow, Whitespace()) |> ignore
