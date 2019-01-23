namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.Rename

open JetBrains.Application
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Refactorings.Rename

type FSharpCustomRenameModel(declaredElement, reference, lifetime, changeNameKind: ChangeNameKind) =
    inherit ClrCustomRenameModel(declaredElement, reference, lifetime)

    member x.ChangeNameKind = changeNameKind


type FSharpAtomicRename(declaredElement, newName, doNotShowBindingConflicts) =
    inherit AtomicRename(declaredElement, newName, doNotShowBindingConflicts)

    let getModel (executor: IRenameRefactoring) =
        match executor.Workflow with
        | :? RenameWorkflowBase as workflow -> workflow.DataModel.Model :?> FSharpCustomRenameModel
        | workflow -> failwithf "Got workflow: %O" workflow

    override x.SetName(declaration, executor) =
        match declaration with
        | :? IFSharpDeclaration as fsDeclaration ->
            let model = getModel executor
            fsDeclaration.SetName(x.NewName, model.ChangeNameKind)
        | declaration -> failwithf "Got declaration: %O" declaration


[<Language(typeof<FSharpLanguage>)>]
type RenameHelper() =
    inherit RenameHelperBase()

    override x.IsLanguageSupported = true

    override x.IsCheckResolvedTo(newReference: IReference, newDeclaredElement: IDeclaredElement) =
        newDeclaredElement :? IFSharpDeclaredElement ||
        base.IsCheckResolvedTo(newReference, newDeclaredElement)

    override x.IsLocalRename(element: IDeclaredElement) = element :? ILocalDeclaration
    override x.CheckLocalRenameSameDocument(element: IDeclaredElement) = x.IsLocalRename(element)

    override x.GetOptionsModel(declaredElement, reference, lifetime) =
        FSharpCustomRenameModel(declaredElement, reference, lifetime, (* todo *) ChangeNameKind.SourceName) :> _


[<ShellFeaturePart>]
type FSharpAtomicRenamesFactory() =
    inherit AtomicRenamesFactory()

    override x.IsApplicable(element: IDeclaredElement) =
        element.PresentationLanguage.Is<FSharpLanguage>()

    override x.CheckRenameAvailability(element: IDeclaredElement) =
        match element with
        | :? ILocalDeclaration -> RenameAvailabilityCheckResult.CanBeRenamed
        | _ -> RenameAvailabilityCheckResult.CanNotBeRenamed

    override x.CreateAtomicRenames(declaredElement, newName, doNotAddBindingConflicts) =
        [| FSharpAtomicRename(declaredElement, newName, doNotAddBindingConflicts) :> AtomicRenameBase |] :> _

