namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<AbstractClass>]
type ReplaceWithInnerTreeNodeFixBase(parentNode: IFSharpTreeNode, innerNode: IFSharpTreeNode) =
    inherit FSharpQuickFixBase()

    override x.IsAvailable _ =
        isValid parentNode && isValid innerNode

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(parentNode.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let parenExprIndent = parentNode.Indent
        let innerExprIndent = innerNode.Indent
        let indentDiff = parenExprIndent - innerExprIndent

        if isIdentifierOrKeyword (parentNode.GetPreviousToken()) then
            ModificationUtil.AddChildBefore(parentNode, Whitespace()) |> ignore

        if isIdentifierOrKeyword (parentNode.GetNextToken()) then
            ModificationUtil.AddChildAfter(parentNode, Whitespace()) |> ignore

        let expr = ModificationUtil.ReplaceChild(parentNode, innerNode.Copy())
        shiftNode indentDiff expr
