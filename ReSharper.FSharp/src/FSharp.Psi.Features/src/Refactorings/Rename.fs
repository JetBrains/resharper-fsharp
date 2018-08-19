namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.Rename

open JetBrains.Application
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename
open JetBrains.ReSharper.Psi.Resolve

[<Language(typeof<FSharpLanguage>)>]
type RenameHelper() =
    inherit RenameHelperBase()

    override x.IsLanguageSupported = true

    override x.IsCheckResolvedTo(newReference: IReference, newDeclaredElement: IDeclaredElement) =
        newDeclaredElement :? IFSharpDeclaredElement ||
        base.IsCheckResolvedTo(newReference, newDeclaredElement)

    override x.IsLocalRename(element: IDeclaredElement) = element :? ILocalDeclaration
    override x.CheckLocalRenameSameDocument(element: IDeclaredElement) = x.IsLocalRename(element)


[<ShellFeaturePart>]
type FSharpAtomicRenamesFactory() =
    inherit AtomicRenamesFactory()

    override x.IsApplicable(element: IDeclaredElement) =
        element.PresentationLanguage.Is<FSharpLanguage>()

    override x.CheckRenameAvailability(element: IDeclaredElement) =
        match element with
        | :? ILocalDeclaration -> RenameAvailabilityCheckResult.CanBeRenamed
        | _ -> RenameAvailabilityCheckResult.CanNotBeRenamed
