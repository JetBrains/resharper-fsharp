namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.Rename

open System
open JetBrains.Application
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Refactorings.Rename
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

[<AutoOpen>]
module Util =
    open JetBrains.Util.Logging

    type IRenameWorkflow with
        member x.RenameWorkflow =
            match x with
            | :? RenameWorkflow as workflow -> workflow
            | _ -> failwithf "Got workflow: %O" x

        member x.RenameDataModel =
            x.RenameWorkflow.DataModel

        member x.FSharpChangeNameKind =
            match x.RenameWorkflow.DataModel.Model with
            | :? FSharpCustomRenameModel as fsRenameModel -> fsRenameModel.ChangeNameKind
            | model ->

            let logger = Logger.GetLogger<FSharpCustomRenameModel>()
            logger.Warn(sprintf "Got custom rename model %O, workflow: %O" model x.RenameWorkflow)
            ChangeNameKind.SourceName

    type RenameDataModel with
        member x.FSharpRenameModel =
            x.Model :?> FSharpCustomRenameModel


type FSharpCustomRenameModel(declaredElement, reference, lifetime, changeNameKind: ChangeNameKind) =
    inherit ClrCustomRenameModel(declaredElement, reference, lifetime)

    member x.ChangeNameKind = changeNameKind


type FSharpAtomicRename(declaredElement, newName, doNotShowBindingConflicts) =
    inherit AtomicRename(declaredElement, newName, doNotShowBindingConflicts)

    override x.SetName(declaration, renameRefactoring) =
        match declaration with
        | :? IFSharpDeclaration as fsDeclaration ->
            fsDeclaration.SetName(x.NewName, renameRefactoring.Workflow.FSharpChangeNameKind)
        | declaration -> failwithf "Got declaration: %O" declaration


[<Language(typeof<FSharpLanguage>)>]
type FSharpRenameHelper() =
    inherit RenameHelperBase()

    override x.IsLanguageSupported = true

    override x.IsCheckResolvedTo(newReference: IReference, newDeclaredElement: IDeclaredElement) =
        newDeclaredElement :? IFSharpDeclaredElement ||
        base.IsCheckResolvedTo(newReference, newDeclaredElement)

    override x.IsLocalRename(element: IDeclaredElement) = element :? IFSharpLocalDeclaration
    override x.CheckLocalRenameSameDocument(element: IDeclaredElement) = x.IsLocalRename(element)

    override x.GetSecondaryElements(element: IDeclaredElement) =
        match element with
        | :? ILocalNamedPat as localNamedPat ->
            let mutable pat = localNamedPat :> ISynPat
            while (pat.Parent :? ISynPat) && not (pat.Parent :? ILongIdentPat && (pat.Parent :?> ISynPat).IsDeclaration) do
                pat <- pat.Parent :?> ISynPat

            let declarartions = pat.Declarations |> Array.ofSeq

            declarartions
            |> Array.filter (fun d -> d.DeclaredName = localNamedPat.ShortName)
            |> Seq.cast<IDeclaredElement>

        | _ -> EmptyArray.Instance :> _
    
    override x.GetOptionsModel(declaredElement, reference, lifetime) =
        FSharpCustomRenameModel(declaredElement, reference, lifetime, (* todo *) ChangeNameKind.SourceName) :> _

    override x.GetInitialPage(workflow) =
        let dataModel = workflow.RenameDataModel
        let declaredElement = dataModel.InitialDeclaredElement

        match declaredElement.As<IFSharpDeclaredElement>() with
        | null -> failwithf "Got declared element: %O" declaredElement
        | fsDeclaredElement ->

        dataModel.InitialName <-
            match workflow.FSharpChangeNameKind with
            | ChangeNameKind.SourceName
            | ChangeNameKind.UseSingleName -> fsDeclaredElement.SourceName
            | _ -> fsDeclaredElement.ShortName

        null            


[<ShellFeaturePart>]
type FSharpAtomicRenamesFactory() =
    inherit AtomicRenamesFactory()

    override x.IsApplicable(element: IDeclaredElement) =
        element.PresentationLanguage.Is<FSharpLanguage>()

    override x.CheckRenameAvailability(element: IDeclaredElement) =
        match element with
        | :? IFSharpLocalDeclaration -> RenameAvailabilityCheckResult.CanBeRenamed
        | _ -> RenameAvailabilityCheckResult.CanBeRenamed // todo: add more checks (e.g. for active pattern)

    override x.CreateAtomicRenames(declaredElement, newName, doNotAddBindingConflicts) =
        [| FSharpAtomicRename(declaredElement, newName, doNotAddBindingConflicts) :> AtomicRenameBase |] :> _


[<Language(typeof<FSharpLanguage>)>]
type FSharpNamingService(language: FSharpLanguage) =
    inherit NamingLanguageServiceBase(language)

    override x.MangleNameIfNecessary(name, _) =
        Keywords.QuoteIdentifierIfNeeded name
