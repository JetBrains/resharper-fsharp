namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.Rename

open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.PrettyNaming
open JetBrains.Application
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Naming.Interfaces
open JetBrains.ReSharper.Refactorings.Rename
open JetBrains.Util

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

    override x.IsCheckResolvedTo(newReference, newDeclaredElement) =
        // We have to change the reference so it resolves to the new element.
        // We don't, however, want to actually resolve it and to wait for FCS to type check all the needed projects
        // so assume it's resolved as a workaround.
        newDeclaredElement.PresentationLanguage.Is<FSharpLanguage>()

    override x.IsLocalRename(element: IDeclaredElement) =
        match element with
        | :? ILongIdentPat as longIdentPat -> longIdentPat.IsDeclaration
        | _ -> element :? IFSharpLocalDeclaration

    override x.CheckLocalRenameSameDocument(element: IDeclaredElement) = x.IsLocalRename(element)

    override x.GetSecondaryElements(element: IDeclaredElement) =
        match element with
        | :? ILocalNamedPat as localNamedPat ->
            let mutable pat = localNamedPat :> ISynPat
            while (pat.Parent :? ISynPat) && not (pat.Parent :? ILongIdentPat && (pat.Parent :?> ISynPat).IsDeclaration) do
                pat <- pat.Parent :?> ISynPat

            pat.Declarations
            |> Seq.cast<IDeclaredElement>
            |> Seq.filter (fun decl -> decl != element && decl.ShortName = element.ShortName)

        | :? IUnionCase as unionCase ->
            unionCase.GetGeneratedMembers()

        | :? IGeneratedConstructorParameterOwner as parameterOwner ->
            [| parameterOwner.GetParameter() :> IDeclaredElement |] :> _

        | _ -> EmptyArray.Instance :> _

    override x.GetOptionsModel(declaredElement, reference, lifetime) =
        FSharpCustomRenameModel(declaredElement, reference, lifetime, (* todo *) ChangeNameKind.SourceName) :> _

    override x.GetInitialPage(workflow) =
        let dataModel = workflow.RenameDataModel
        match dataModel.InitialDeclaredElement with
        | null -> null
        | element ->

        dataModel.InitialName <-
            match element.As<IFSharpDeclaredElement>() with
            | null -> element.ShortName
            | fsDeclaredElement ->

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
        | :? FSharpGeneratedMemberBase -> RenameAvailabilityCheckResult.CanNotBeRenamed
        | :? ILongIdentPat as pat when not pat.IsDeclaration -> RenameAvailabilityCheckResult.CanNotBeRenamed

        | :? IFSharpDeclaredElement as fsElement when fsElement.SourceName = SharedImplUtil.MISSING_DECLARATION_NAME ->
            RenameAvailabilityCheckResult.CanNotBeRenamed

        | _ ->

        match element.ShortName with
        | SharedImplUtil.MISSING_DECLARATION_NAME -> RenameAvailabilityCheckResult.CanNotBeRenamed
        | name when IsActivePatternName name -> RenameAvailabilityCheckResult.CanNotBeRenamed
        | _ -> RenameAvailabilityCheckResult.CanBeRenamed

    override x.CreateAtomicRenames(declaredElement, newName, doNotAddBindingConflicts) =
        [| FSharpAtomicRename(declaredElement, newName, doNotAddBindingConflicts) :> AtomicRenameBase |] :> _


[<Language(typeof<FSharpLanguage>)>]
type FSharpNamingService(language: FSharpLanguage) =
    inherit ClrNamingLanguageServiceBase(language)

    let (|Word|_|) word (nameRoot: NameInnerElement) =
        match nameRoot.As<NameWord>() with
        | null -> None
        | nameWord -> if nameWord.Text = word then someUnit else None

    let withWords words (nameRoot: NameRoot) =
        NameRoot(Array.ofList words, nameRoot.PluralityKind, nameRoot.IsFinalPresentation)

    override x.MangleNameIfNecessary(name, _) =
        Keywords.QuoteIdentifierIfNeeded name

    override x.SuggestRoots(element: IDeclaredElement, policyProvider: INamingPolicyProvider) =
        let baseRoots = base.SuggestRoots(element, policyProvider)
        if not (startsWith "FSharp" element.ShortName) then baseRoots else

        match element.As<ITypeElement>() with
        | null -> baseRoots
        | _ ->

        match List.ofSeq baseRoots with
        | [nameRoot] ->
            match List.ofSeq nameRoot.Words with
            | Word "F" :: Word "Sharp" :: rest -> [| withWords rest nameRoot |] :> _
            | _ -> baseRoots
        | _ -> baseRoots

    override x.IsSameNestedNameAllowedForMembers = true


[<RenamePart>]
type FSharpDeclaredElementForRenameProvider() =
    interface IPrimaryDeclaredElementForRenameProvider with
        member x.GetPrimaryDeclaredElement(element, reference) =
            match element.As<IFSharpGeneratedFromOtherElement>() with
            | null -> element
            | generated ->

            match generated.OriginElement with
            | null -> element
            | origin -> origin :> _
