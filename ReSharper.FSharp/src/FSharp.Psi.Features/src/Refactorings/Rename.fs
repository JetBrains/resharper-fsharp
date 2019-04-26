namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.Rename

open System
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.PrettyNaming
open JetBrains.Application
open JetBrains.IDE.UI.Extensions.Validation
open JetBrains.ReSharper.Feature.Services.Refactorings
open JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Naming.Interfaces
open JetBrains.ReSharper.Refactorings.Rename
open JetBrains.ReSharper.Refactorings.Rename.Pages
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
type FSharpRenameHelper(namingService: FSharpNamingService) =
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
        | :? ITypeParameter -> true
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

    override x.IsValidName(decl: IDeclaration, elementType: DeclaredElementType, name: string) =
        namingService.IsValidName(decl.DeclaredElement, name)

    override x.GetInitialPage(workflow) =
        let page: IRefactoringPage =
            { new RenameInitialControlPage(workflow.RenameWorkflow) with
                  override x.AddCustomValidation(textBox, element) =
                      let validate = Func<_,_>(fun property ->
                          FSharpNameValidationRule(property, element, namingService) :> ValidationRuleWithProperty<_>)
                      textBox.WithValidationRule(x.Lifetime, validate) |> ignore } :> _

        let dataModel = workflow.RenameDataModel
        match dataModel.InitialDeclaredElement with
        | null -> page
        | element ->

        dataModel.InitialName <-
            match element.As<IFSharpDeclaredElement>() with
            | null -> element.ShortName
            | fsDeclaredElement ->

            match workflow.FSharpChangeNameKind with
            | ChangeNameKind.SourceName
            | ChangeNameKind.UseSingleName -> fsDeclaredElement.SourceName
            | _ -> fsDeclaredElement.ShortName

        page


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
        | :? ILongIdentPat as pat when not pat.IsDeclaration -> RenameAvailabilityCheckResult.CanNotBeRenamed

        | :? IFSharpDeclaredElement as fsElement when fsElement.SourceName = SharedImplUtil.MISSING_DECLARATION_NAME ->
            RenameAvailabilityCheckResult.CanNotBeRenamed

        | :? IModule as fsModule when fsModule.IsAnonymous ->
            RenameAvailabilityCheckResult.CanNotBeRenamed // todo: needs a special implementation

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

    let notAllowedInTypes =
        // F# 4.1 spec: 3.4 Identifiers and Keywords
        [| '.'; '+'; '$'; '&'; '['; ']'; '/'; '\\'; '*'; '\"'; '`' |]

    let withWords words (nameRoot: NameRoot) =
        NameRoot(Array.ofList words, nameRoot.PluralityKind, nameRoot.IsFinalPresentation)

    let withSuffix (AsList suffix) (nameRoot: NameRoot) =
        let words = List.ofSeq nameRoot.Words
        let words = words @ suffix
        withWords words nameRoot

    let (|Word|_|) word (nameElement: NameInnerElement) =
        match nameElement.As<NameWord>() with
        | null -> None
        | nameWord -> if nameWord.Text = word then someUnit else None

    let (|FSharpNameRoot|_|) (root: NameRoot) =
        match List.ofSeq root.Words with
        | Word "F" :: Word "Sharp" :: rest -> Some (withWords rest root)
        | _ -> None

    let dropFSharpWords root =
        match root with
        | FSharpNameRoot root -> root
        | _ -> root

    let isFSharpTypeLike (element: IDeclaredElement) =
        element :? ITypeElement && startsWith "FSharp" element.ShortName

    override x.MangleNameIfNecessary(name, _) =
        Keywords.QuoteIdentifierIfNeeded name

    override x.SuggestRoots(typ: IType, policyProvider: INamingPolicyProvider) =
        let roots = base.SuggestRoots(typ, policyProvider)

        match typ.As<IDeclaredType>() with
        | null -> roots
        | declaredType ->

        let typeElement = declaredType.GetTypeElement()
        if not (isFSharpTypeLike typeElement) then roots else
        if typeElement.GetClrName().Equals(fsListTypeName) then roots else

        let typeParameters = typeElement.TypeParameters
        if typeParameters.IsEmpty() then roots else

        let psiServices = typeElement.GetPsiServices()
        match psiServices.Naming.Parsing.GetName(typeElement, "unknown", policyProvider).GetRoot() with
        | FSharpNameRoot root ->
            let typeArg = declaredType.GetSubstitution().[typeParameters.[0]]
            let typeArgRoots = x.SuggestRoots(typeArg, policyProvider) |> List.ofSeq
            let newRoots = typeArgRoots |> List.map (withSuffix root.Words)
            seq {
                yield! Seq.map dropFSharpWords roots
                yield! newRoots
            }

        | _ -> roots

    override x.SuggestRoots(element: IDeclaredElement, policyProvider: INamingPolicyProvider) =
        let roots = base.SuggestRoots(element, policyProvider)
        if isFSharpTypeLike element then Seq.map dropFSharpWords roots else roots

    override x.IsSameNestedNameAllowedForMembers = true

    member x.IsValidName(element: IDeclaredElement, name: string) =
        let isValidCaseStart char =
            // F# 4.1 spec: 8.5 Union Type Definitions
            Char.IsUpper(char) && not (Char.IsLower(char))

        let isTypeLike (element: IDeclaredElement) =
            element :? ITypeElement || element :? IUnionCase || element :? INamespace

        let isUnionCaseLike (element: IDeclaredElement) =
            match element with
            | :? IUnionCase
            | :? IActivePatternCase -> true
            | :? ITypeElement as typeElement -> typeElement.IsException()
            | _ -> false

        if name.IsEmpty() then false else
        if isUnionCaseLike element && not (isValidCaseStart name.[0]) then false else
        if isTypeLike element && name.IndexOfAny(notAllowedInTypes) <> -1 then false else

        not (startsWith "`" name || endsWith "`" name || name.ContainsNewLine() || name.Contains("``"))


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
