[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FSharpErrorUtil

open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.Util

let getUpcastRange (upcastExpr: IUpcastExpr) =
    if not (isValid upcastExpr && isValid upcastExpr.OperatorToken) then DocumentRange.InvalidRange else

    let documentRange = upcastExpr.GetNavigationRange()
    let operatorRange = upcastExpr.OperatorToken.GetNavigationRange()

    DocumentRange(documentRange.Document, TextRange(operatorRange.StartOffset.Offset, documentRange.EndOffset.Offset))
