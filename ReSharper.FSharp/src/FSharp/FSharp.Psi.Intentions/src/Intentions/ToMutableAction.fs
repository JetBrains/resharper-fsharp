namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "ToMutable", GroupType = typeof<FSharpContextActions>, Description = "Makes value mutable")>]
type ToMutableAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override x.Text = "To mutable"

    override x.IsAvailable _ =
        let decl = dataProvider.GetSelectedElement<IRecordFieldDeclaration>()
        if not (isValid decl && decl.GetNameRange().Contains(dataProvider.SelectedTreeRange)) then false else

        let field = decl.DeclaredElement.As<IRecordField>()
        isValid field && field.CanBeMutable && not field.IsMutable

    override x.ExecutePsiTransaction _ =
        let fieldDecl = dataProvider.GetSelectedElement<IRecordFieldDeclaration>()
        use writeCookie = WriteLockCookie.Create(fieldDecl.IsPhysical())

        let declaredElement = fieldDecl.DeclaredElement :?> IRecordField
        declaredElement.SetIsMutable(true)


[<ContextAction(Name = "ToImmutableField", GroupType = typeof<FSharpContextActions>, Description = "Makes field immutable")>]
type ToImmutableFieldAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override x.Text = "To immutable"

    override x.IsAvailable _ =
        let decl = dataProvider.GetSelectedElement<IRecordFieldDeclaration>()
        if not (isValid decl && decl.IsMutable) then false else

        let nameRange = decl.GetNameRange()
        let mutableRange = decl.MutableKeyword.GetTreeTextRange()
        let selectedRange = dataProvider.SelectedTreeRange
        if not (nameRange.Contains(selectedRange) || mutableRange.Contains(selectedRange)) then false else

        let field = decl.DeclaredElement.As<IRecordField>()
        isValid field && field.IsMutable

    override x.ExecutePsiTransaction _ =
        let fieldDecl = dataProvider.GetSelectedElement<IRecordFieldDeclaration>()
        use writeCookie = WriteLockCookie.Create(fieldDecl.IsPhysical())

        let declaredElement = fieldDecl.DeclaredElement :?> IRecordField
        declaredElement.SetIsMutable(false)
