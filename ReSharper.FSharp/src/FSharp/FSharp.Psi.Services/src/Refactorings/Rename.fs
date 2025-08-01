namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.Rename

open System
open System.Linq
open FSharp.Compiler.Syntax.PrettyNaming
open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.IDE.UI.Extensions.Validation
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
open JetBrains.ReSharper.Plugins.FSharp.Psi.Searching
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Refactorings.Rename
open JetBrains.ReSharper.Refactorings.Rename.Pages
open JetBrains.ReSharper.Refactorings.RenameModel
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


type FSharpCustomRenameModel(declaredElement, reference, changeNameKind: ChangeNameKind) =
    inherit ClrCustomRenameModel(declaredElement, reference)

    member x.ChangeNameKind = changeNameKind


type FSharpAnonRecordFieldAtomicRename(declaredElement, newName) =
    inherit AnonymousTypePropertyAtomicRenameBase(declaredElement, newName)

    override x.SetName(element, newName) =
        let fieldProperty = element :?> IFSharpAnonRecordFieldProperty
        fieldProperty.SetName(newName) :> _

    override x.CompareReferencesOnMemberDeclarations(_, _) = 0

type FSharpParameterAtomicRename(fsParam, newName, doNotAddBindingConflicts) =
    inherit AtomicRename(fsParam, newName, doNotAddBindingConflicts)

    override this.GetUpdatedPrimaryElement() = fsParam

    override this.GetDeclarations(element) =
        match element with
        | :? IFSharpParameter as fsParam ->
            fsParam.GetParameterOriginDeclarations().OfType<IDeclaration>().AsIList()
        | _ -> EmptyList.Instance


[<Language(typeof<FSharpLanguage>)>]
type FSharpRenameHelper(namingService: FSharpNamingService) =
    inherit RenameHelperBase()

    override x.IsLanguageSupported = true

    override x.IsCheckResolvedTo(_, newDeclaredElement) =
        // Assume the reference is resolved to prevent waiting for FCS type checks.
        newDeclaredElement.PresentationLanguage.Is<FSharpLanguage>() ||

        newDeclaredElement.GetSolution().GetComponent<IFcsAssemblyReaderShim>().IsEnabled &&
        AssemblyReaderShim.supportedLanguages.Contains(newDeclaredElement.PresentationLanguage)

    override x.IsLocalRename(element: IDeclaredElement) =
        if not (element :? IFSharpLocalDeclaration) then false else

        match element with
        | :? IReferencePat as refPat -> refPat.IsDeclaration
        | _ -> true

    override x.CheckLocalRenameSameDocument(element: IDeclaredElement) =
        x.IsLocalRename(element)

    override x.GetSecondaryElements(element: IDeclaredElement, newName) =
        match element with
        | :? ILocalReferencePat as localRefPat ->
            localRefPat.GetPartialDeclarations()
            |> Seq.cast<IDeclaredElement>
            |> Seq.filter (fun decl -> decl != localRefPat)

        | :? IUnionCase as unionCase ->
            unionCase.GetGeneratedMembers()

        | :? IGeneratedConstructorParameterOwner as parameterOwner ->
            match parameterOwner.GetGeneratedParameter() with
            | null -> EmptyArray.Instance :> _
            | parameter -> [| parameter :> IDeclaredElement |] :> _

        | :? IFSharpProperty as property ->
            property.GetExplicitAccessors() |> Seq.cast

        | :? IFSharpModule -> EmptyArray.Instance :> _

        | :? IFSharpSourceTypeElement as fsTypeElement ->
            match fsTypeElement.GetModuleToUpdateName(newName) with
            | null -> EmptyArray.Instance :> _
            | fsModule -> [| fsModule |] :> _

        | :? IFSharpParameter as fsParam ->
            fsParam.GetParameterOriginElements() |> Seq.cast

        | _ -> EmptyArray.Instance :> _

    override this.UpdateSecondaryElement(element, newDeclaredElement) =
        base.UpdateSecondaryElement(element, newDeclaredElement)

    override x.GetOptionsModel(declaredElement, reference, _) =
        FSharpCustomRenameModel(declaredElement, reference, (* todo *) ChangeNameKind.SourceName) :> _

    override x.IsValidName(decl: IDeclaration, _: DeclaredElementType, name: string) =
        namingService.IsValidName(decl.DeclaredElement, name)

    override x.IsValidName(element: IDeclaredElement, _: DeclaredElementType, name: string) =
        namingService.IsValidName(element, name)

    override x.GetInitialPage(workflow) =
        let createPage (workflow: IRenameWorkflow) =
            { new RenameInitialControlPage(workflow.RenameWorkflow) with
                  override x.AddCustomValidation(textBox, element) =
                      let validate = Func<_,ValidationRuleWithProperty<_>>(fun property ->
                          FSharpNameValidationRule(property, element, namingService) :> _)
                      textBox.WithValidationRule(x.Lifetime, validate) |> ignore }

        let dataModel = workflow.RenameDataModel
        match dataModel.InitialDeclaredElement with
        | :? IPathDeclaredElement -> null
        | null -> createPage workflow :> _
        | element ->

        dataModel.InitialName <-
            match element.As<IFSharpDeclaredElement>() with
            | null -> element.ShortName
            | fsDeclaredElement ->

            match workflow.FSharpChangeNameKind with
            | ChangeNameKind.SourceName
            | ChangeNameKind.UseSingleName -> fsDeclaredElement.SourceName
            | _ -> fsDeclaredElement.ShortName

        createPage workflow :> _

    override x.AddExtraNames(namesCollection, declaredElement) =
        match declaredElement with
        | :? INamespace
        | :? ITypeElement -> ()
        | _ ->

        for declaration in declaredElement.GetDeclarations() do
            match declaration with
            | :? IFSharpPattern as pat -> namingService.AddExtraNames(namesCollection, pat)
            | _ -> ()

    override x.GetNameDocumentRangeForRename(declaration: IDeclaration, initialName): DocumentRange =
        match declaration with
        | :? IWildPat as wil -> wil.GetDocumentRange()
        | _ -> base.GetNameDocumentRangeForRename(declaration, initialName)

    override this.SetName(declaration, newName, refactoring) =
        match declaration with
        | :? IFSharpDeclaration as fsDeclaration ->
            fsDeclaration.SetName(newName, refactoring.Workflow.FSharpChangeNameKind)
        | declaration -> failwithf "Got declaration: %O" declaration


type FSharpNameValidationRule(property, element: IDeclaredElement, namingService: FSharpNamingService) as this =
    inherit SimpleValidationRuleOnProperty<string>(property, element.GetSolution().Locks)

    do
        this.Message <- "Identifier is not valid"
        this.ValidateAction <- Func<_,_>(fun name -> namingService.IsValidName(element, name))


[<ShellFeaturePart>]
type FSharpAtomicRenamesFactory() =
    inherit AtomicRenamesFactory()

    override x.IsApplicable(element: IDeclaredElement) =
        element.PresentationLanguage.Is<FSharpLanguage>()

    override x.CheckRenameAvailability(element: IDeclaredElement) =
        match element with
        | :? FSharpGeneratedMemberBase -> RenameAvailabilityCheckResult.CanNotBeRenamed
        | :? IReferencePat as refPat when not refPat.IsDeclaration -> RenameAvailabilityCheckResult.CanNotBeRenamed
        | :? IWildPat -> RenameAvailabilityCheckResult.CanBeRenamed

        | :? IFSharpDeclaredElement as fsElement when fsElement.SourceName = SharedImplUtil.MISSING_DECLARATION_NAME ->
            RenameAvailabilityCheckResult.CanNotBeRenamed

        | :? IFSharpModule as fsModule when fsModule.IsAnonymous ->
            RenameAvailabilityCheckResult.CanNotBeRenamed // todo: needs a special implementation

        | _ ->

        match element.ShortName with
        | SharedImplUtil.MISSING_DECLARATION_NAME -> RenameAvailabilityCheckResult.CanNotBeRenamed
        | name when IsActivePatternName name -> RenameAvailabilityCheckResult.CanNotBeRenamed
        | _ -> RenameAvailabilityCheckResult.CanBeRenamed

    override x.CreateAtomicRenames(declaredElement, newName, doNotAddBindingConflicts) =
        match declaredElement with
        | :? IFSharpParameter -> [| FSharpParameterAtomicRename(declaredElement, newName, doNotAddBindingConflicts) |]
        | :? IFSharpAnonRecordFieldProperty -> [| FSharpAnonRecordFieldAtomicRename(declaredElement, newName) |]
        | _ -> [| AtomicRename(declaredElement, newName, doNotAddBindingConflicts) |]


[<RenamePart>]
type FSharpDeclaredElementForRenameProvider() =
    let (|SecondaryElementOrigin|_|) (element: IDeclaredElement) =
        match element with
        | :? ISecondaryDeclaredElement as sde -> sde.OriginElement |> Option.ofObj
        | _ -> None

    let (|FSharpParameter|_|) (element: IDeclaredElement) =
        match element with
        | :? IReferencePat as refPat -> refPat.TryGetDeclaredFSharpParameter() |> Option.ofObj
        | :? IParameterSignatureTypeUsage as paramSigTypeUsage -> paramSigTypeUsage.FSharpParameter |> Option.ofObj
        | _ -> None

    interface IPrimaryDeclaredElementForRenameProvider with
        member x.GetPrimaryDeclaredElement(element, _) =
            match element with
            | :? IDotLambdaId
            | :? IProvidedSecondaryDeclaredElement -> null
            | SecondaryElementOrigin origin -> origin
            | FSharpParameter fsParam -> fsParam
            | _ -> element


[<DerivedRenamesEvaluator>]
type SingleUnionCaseRenameEvaluator() =
    interface IDerivedRenamesEvaluator with
        member x.SuggestedElementsHaveDerivedName = false
        member x.CreateFromReference(_, _, _) = EmptyList.Instance :> _

        member x.CreateFromElement(initialElement, _, _) =
            let isApplicable (typeElement: ITypeElement) =
                typeElement.IsUnion() &&

                let sourceName = typeElement.GetSourceName()
                sourceName <> SharedImplUtil.MISSING_DECLARATION_NAME &&

                let unionCases = typeElement.GetSourceUnionCases()
                unionCases.Count = 1 && unionCases[0].SourceName = sourceName

            match initialElement.FirstOrDefault() with
            | :? ITypeElement as typeElement when isApplicable typeElement ->
                [| typeElement.GetSourceUnionCases().[0] :> IDeclaredElement |] :> _

            | :? IUnionCase as unionCase ->
                let containingType = unionCase.GetContainingType().NotNull()
                if not (isApplicable containingType) then [] :> _ else
                [| containingType :> IDeclaredElement |] :> _

            | _ -> [] :> _


[<DerivedRenamesEvaluator>]
type AssociatedTypeRenameEvaluator() =
    interface IDerivedRenamesEvaluator with
        member x.SuggestedElementsHaveDerivedName = false
        member x.CreateFromReference(_, _, _) = EmptyList.Instance :> _

        member x.CreateFromElement(initialElement, _, _) =
            match initialElement.FirstOrDefault() with
            | :? IFSharpModule as fsModule ->
                let associatedTypeElement = fsModule.AssociatedTypeElement
                [ if isNotNull associatedTypeElement then
                    associatedTypeElement :> IDeclaredElement ] :>  _

            | :? IFSharpTypeElement as fsTypeElement ->
                let containingEntityNestedTypes: ITypeElement seq =
                    match fsTypeElement.GetContainingType() with
                    | null ->
                        let ns = fsTypeElement.GetContainingNamespace()
                        ns.GetNestedTypeElements(getModuleOnlySymbolScope fsTypeElement.Module false) :> _

                    | containingType ->
                        containingType.NestedTypes :> _

                let associatedModule =
                    containingEntityNestedTypes |> Seq.tryFind (fun typeElement ->
                        let fsModule = typeElement.As<IFSharpModule>()

                        isNotNull fsModule &&
                        fsModule.SourceName = fsTypeElement.SourceName &&
                        fsTypeElement.Equals(fsModule.AssociatedTypeElement))

                associatedModule |> Option.toList |> Seq.cast

            | _ -> [] :> _

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type FSharpSuspiciousReferenceSearchService(searchFilter: FSharpSearchFilter) =
    inherit SuspiciousReferencesSearchService()

    override this.TryGetKey(declaredElement, _) = searchFilter.TryGetKey(declaredElement)
    override this.CanContainSuspiciousReferences(sourceFile, key) = searchFilter.CanContainReferences(sourceFile, key)

