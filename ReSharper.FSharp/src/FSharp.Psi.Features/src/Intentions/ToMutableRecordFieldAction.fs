namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

[<ContextAction(Name = "ToMutableRecordField", Group = "F#", Description = "Makes record field mutable")>]
type ToMutableRecordFieldAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override x.Text = "To mutable"

    override x.IsAvailable _ =
        let fieldDecl = dataProvider.GetSelectedElement<IRecordFieldDeclaration>()
        if not (isValid fieldDecl && x.IsAtTreeNode(fieldDecl.Identifier)) then false else

        let field = fieldDecl.DeclaredElement.As<IRecordField>()
        isValid field && not field.IsMutable

    override x.ExecutePsiTransaction(_, _) =
        let fieldDecl = dataProvider.GetSelectedElement<IRecordFieldDeclaration>()
        let field = fieldDecl.DeclaredElement :?> IRecordField
        field.SetIsMutable(true)

        null
