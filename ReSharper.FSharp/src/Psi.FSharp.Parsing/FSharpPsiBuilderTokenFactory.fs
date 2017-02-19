namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.ReSharper.Psi.TreeBuilder
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.FSharp.Impl.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

type FSharpPsiBuilderTokenFactory() =
    interface IPsiBuilderTokenFactory with
        member this.CreateToken(tokenType, buffer, startOffset, endOffset) =
            let startOffset = TreeOffset(startOffset)
            let endOffset = TreeOffset(endOffset)
            
            if obj.ReferenceEquals(tokenType, FSharpTokenType.IDENTIFIER) ||
               obj.ReferenceEquals(tokenType, FSharpTokenType.OPERATOR)
            then FSharpIdentifierToken(tokenType, buffer, startOffset, endOffset) :> LeafElementBase
            else tokenType.Create(buffer, startOffset, endOffset)