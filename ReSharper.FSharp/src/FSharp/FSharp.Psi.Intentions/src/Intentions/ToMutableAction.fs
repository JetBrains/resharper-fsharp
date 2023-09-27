namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
open JetBrains.ReSharper.Psi.Tree

// todo: fix mutable inside binding range, then replace IRecordField usage below

[<ContextAction(Name = "ToMutable", Group = "F#", Description = "Makes value mutable")>]
type ToMutableAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override x.Text = "To mutable"

    override x.IsAvailable _ =
        let decl = dataProvider.GetSelectedElement<IDeclaration>()
        if not (isValid decl && decl.GetNameRange().Contains(dataProvider.SelectedTreeRange)) then false else

        let declaredElement = decl.DeclaredElement
        if not (isValid declaredElement) then false else

        let declaredElement = declaredElement.As<IRecordField>()
        isNotNull declaredElement && declaredElement.CanBeMutable && not declaredElement.IsMutable

    override x.ExecutePsiTransaction(_, _) =
        let fieldDecl = dataProvider.GetSelectedElement<IDeclaration>()
        let declaredElement = fieldDecl.DeclaredElement :?> IRecordField
        declaredElement.SetIsMutable(true)

        null
