module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpLambdaUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

let deleteLastArgs (lambda: ILambdaExpr) (pats: TreeNodeCollection<IFSharpPattern>) count =
    let arrow = lambda.RArrow
    let nodeAfterPats = lambda.Parameters.NextSibling
    let firstNodeToDelete = pats.[pats.Count - count - 1].NextSibling
    let lastNodeToDelete = pats.Last()

    deleteChildRange firstNodeToDelete lastNodeToDelete

    if nodeAfterPats != arrow then replaceRangeWithNode nodeAfterPats arrow.PrevSibling (Whitespace(1))
    else ModificationUtil.AddChildBefore(arrow, Whitespace(1)) |> ignore
