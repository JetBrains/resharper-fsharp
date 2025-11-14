namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

[<Language(typeof<FSharpLanguage>)>]
type FSharpDebuggerLocalSymbolProvider2() =
    interface IDebuggerLocalSymbolProvider with
        member this.FindLocalDeclarationAt(file, range, name) =
            let token = file.FindTokenAt(range.StartOffset)
            if isNull token then null, null else

            let values = LocalValuesUtil.getLocalValues token
            match values.TryGetValue(name) with
            | false, _ -> null, null
            | true, (decl, _) -> decl, decl.DeclaredElement

        member this.FindContainingFunctionDeclarationBody(node) =
            match node.GetContainingNode<IChameleonExpression>() with
            | null -> node.GetContainingFile()
            | chameleonExpr -> chameleonExpr
