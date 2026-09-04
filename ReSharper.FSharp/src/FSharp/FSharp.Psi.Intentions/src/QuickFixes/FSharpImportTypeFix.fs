namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Resources
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
// TODO: refactor in the platform
open JetBrains.ReSharper.Psi.CSharp.Util
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Tree

[<RequireQualifiedAccess>]
module FSharpImportTypeFix =
    let private isNameofArgument (refExpr: IReferenceExpr) =
        let appExpr = PrefixAppExprNavigator.GetByArgumentExpression(refExpr.IgnoreParentParens())
        if isNull appExpr then false else

        match appExpr.FunctionExpression with
        | :? IReferenceExpr as funExpr -> isPredefinedFunctionRef "nameof" funExpr
        | _ -> false

    let private isApplicable (owner: IFSharpReferenceOwner) (typeElement: ITypeElement) =
        if isNull owner then true
        elif owner.GetNextMeaningfulToken() |> getTokenType == FSharpTokenType.DOT then true else

        match owner with
        | :? IReferenceExpr as refExpr ->
            match typeElement with
            | :? IFSharpModule -> isNameofArgument refExpr
            | :? IClass as c when c.IsStaticClass() -> isNameofArgument refExpr
            | _ -> true

        | :? ITypeReferenceName as typeReferenceName ->
            match typeElement with
            | :? IFSharpModule ->
                isNotNull (ModuleAbbreviationDeclarationNavigator.GetByTypeName(typeReferenceName))
            | :? IClass as c when c.IsStaticClass() ->
                isNotNull (TypeExtensionDeclarationNavigator.GetByIdentifier(typeReferenceName.Identifier))
            | _ -> true

        | _ -> true

    let filterConflicts (typeElements: ITypeElement seq) (reference: IReference) =
        match reference.GetTreeNode() with
        | :? IFSharpReferenceOwner as owner ->
            typeElements |> Seq.filter (isApplicable owner)
        | _ ->
            typeElements

    let doAdditionalSorting (reference: IReference) (candidates: ITypeElement seq) =
        let referenceOwner = reference.GetTreeNode().As<IFSharpReferenceOwner>()

        let rate (typeElement: ITypeElement) =
            let isApplicable = isApplicable referenceOwner typeElement
            let isLocal =
                match typeElement with
                | :? IFSharpTypeElement as t -> t.GetFSharpAccessRights().IsFilePrivate
                | _ -> false

            (if isApplicable then 1 else 0), (if isLocal then 1 else 0)

        candidates |> Seq.sortByDescending rate

type FSharpImportTypeFix(reference) =
    inherit ImportTypeFix(reference)

    override this.Format(typeElement: ITypeElement) =
        if typeElement :? IFSharpModule then Strings.FSharpImportModule_Text
        else base.Format(typeElement)

    override this.GetSymbolScope(context) =
        let symbolCache = context.GetPsiServices().Symbols
        symbolCache.GetAlternativeNamesSymbolScope(context.GetPsiModule(), true)

    override this.DoAdditionalOrdering(candidates) =
        FSharpImportTypeFix.doAdditionalSorting reference candidates

type FSharpPopupImportTypeFix(reference) =
    inherit ImportTypeQuickPopupFix(reference)

    override this.FilterConflicts(typeElements, reference) =
        FSharpImportTypeFix.filterConflicts typeElements reference

    override this.GetSymbolScope(context) =
        let symbolCache = context.GetPsiServices().Symbols
        symbolCache.GetAlternativeNamesSymbolScope(context.GetPsiModule(), true)

    override this.DoAdditionalOrdering(candidates) =
        FSharpImportTypeFix.doAdditionalSorting reference candidates


type FSharpReferenceModuleAndTypeFix(reference) =
    inherit ReferenceModuleAndTypeFix(reference)

    override this.IsAvailable(cache) =
        base.IsAvailable(cache) &&
        not (FSharpImportStaticMemberUtil.isAvailable reference)

    override this.GetSymbolScope(context) =
        let symbolCache = context.GetPsiServices().Symbols
        symbolCache.GetAlternativeNamesSymbolScope(LibrarySymbolScope.FULL)

    override this.DoAdditionalOrdering(candidates) =
        FSharpImportTypeFix.doAdditionalSorting reference candidates
