module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpBindUtil

open System
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util

let rec private getFirstNameReference (reference: FSharpSymbolReference) =
    match reference.QualifierReference with
    | null -> reference
    | qualifierReference -> getFirstNameReference qualifierReference

let rec private setQualifiers (reference: FSharpSymbolReference) (elements: IClrDeclaredElement list) =
    match elements with
    | [] -> ()
    | element :: outerElements ->

    reference.SetQualifier(element)
    setQualifiers reference.QualifierReference outerElements

let private qualifyIfPreferred (context: ITreeNode) (reference: FSharpSymbolReference) (moduleToOpen: IClrDeclaredElement) =
    let settings = context.GetSettingsStoreWithEditorConfig()
    let preferQualified = settings.GetValue(fun key -> key.PreferQualifiedReference)
    if not preferQualified then false else

    let firstNameReference = getFirstNameReference reference
    let hasExprQualifier = firstNameReference.IsQualified
    if hasExprQualifier then false else

    tryGetElementsToOpen context moduleToOpen
    |> Option.iter (fun elementsToOpen -> setQualifiers firstNameReference (List.rev elementsToOpen))
    true

let private isExtensionMember (declaredElement: IClrDeclaredElement) =
    match declaredElement with
    | :? ITypeMember as typeMember -> typeMember.ExtensionMemberKind <> ExtensionMemberKind.NONE
    | _ -> false

let private qualifyOrAddOpen (reference: FSharpSymbolReference) (moduleToOpen: IClrDeclaredElement) =
    let referenceOwner = reference.GetElement()
    let qualified = qualifyIfPreferred referenceOwner reference moduleToOpen
    if not qualified then
        addOpen referenceOwner moduleToOpen

// todo: we modify the tree and then ask FCS. What tree is used? Do we wait for the new analysis?
let bindDeclaredElementToReference (context: ITreeNode) (reference: FSharpSymbolReference)
        (declaredElement: IClrDeclaredElement) opName =
    reference.SetRequiredQualifiers(declaredElement, context)

    let referenceOwner = reference.GetElement()
    let resolveExpr = referenceOwner.ReferenceContext <> Nullable FSharpReferenceContext.Type
    if FSharpResolveUtil.resolvesToQualified declaredElement reference resolveExpr opName then () else

    match tryGetModuleToOpen declaredElement with
    | None -> ()
    | Some moduleToOpen ->

    if isExtensionMember declaredElement then
        addOpen referenceOwner moduleToOpen
    else
        qualifyOrAddOpen reference moduleToOpen

let bindModuleMemberToReference (reference: FSharpSymbolReference) (containingType: ITypeElement) =
    let referenceOwner = reference.GetElement()
    reference.SetRequiredQualifiersForContainingType(containingType, referenceOwner)

    let moduleToOpen = getModuleToOpenFromContainingType containingType
    qualifyOrAddOpen reference moduleToOpen

let bindFcsSymbolToReference (context: ITreeNode) (reference: FSharpSymbolReference) (fcsSymbol: FSharpSymbol) opName =
    let declaredElement = fcsSymbol.GetDeclaredElement(context.GetPsiModule()).As<IClrDeclaredElement>()
    if isNull reference || reference.IsQualified || isNull declaredElement then () else
    bindDeclaredElementToReference context reference declaredElement opName

let bindFcsSymbolToReferenceAndGetModuleToOpen (openedModulesProvider: OpenedModulesProvider)
        (reference: FSharpSymbolReference) (fcsSymbol: FSharpSymbol) =
    let context = openedModulesProvider.Context
    let declaredElement = fcsSymbol.GetDeclaredElement(context.GetPsiModule()).As<IClrDeclaredElement>()
    if isNull declaredElement then None else

    reference.SetRequiredQualifiers(declaredElement, context)

    match tryGetModuleToOpen declaredElement with
    | None -> None
    | Some moduleToOpen ->

    if FSharpImportUtil.isTypeOrNsInScope openedModulesProvider moduleToOpen then None else

    let qualified = qualifyIfPreferred context reference moduleToOpen
    if qualified then None else Some moduleToOpen


// todo: replace Fcs symbols with R# elements when possible
let bindFcsSymbol (pattern: IFSharpPattern) (fcsSymbol: FSharpSymbol) opName =
    // todo: move to reference binding
    let bind name =
        let factory = pattern.CreateElementFactory()

        let name = FSharpNamingService.normalizeBackticks name
        let newPattern = factory.CreatePattern(name, false)
        let pat = ModificationUtil.ReplaceChild(pattern, newPattern)

        let referenceName = FSharpPatternUtil.getReferenceName pat

        let oldQualifierWithDot =
            let referenceName = FSharpPatternUtil.getReferenceName pattern
            if isNotNull referenceName then TreeRange(referenceName.Qualifier, referenceName.Delimiter) else null

        if isNotNull oldQualifierWithDot then
            ModificationUtil.AddChildRangeAfter(referenceName, null, oldQualifierWithDot) |> ignore

        bindFcsSymbolToReference pat referenceName.Reference fcsSymbol opName

        pat
    
    match fcsSymbol with
    | :? FSharpUnionCase as unionCase -> bind unionCase.Name
    | :? FSharpField as field when isEnumMember field -> bind field.Name
    | _ -> failwith $"Unexpected symbol: {fcsSymbol}"
