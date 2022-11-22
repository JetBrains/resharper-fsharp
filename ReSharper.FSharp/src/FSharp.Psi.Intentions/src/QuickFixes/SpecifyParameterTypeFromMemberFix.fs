namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.UI.Tooltips
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Naming.Settings
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText

type SpecifyParameterTypeFromMemberFix(refExpr: IReferenceExpr) =
    inherit FSharpQuickFixBase()

    let mutable typeElement = null

    member val QualifierRefExpr =
        if isNotNull refExpr then refExpr.Qualifier.As<IReferenceExpr>() else null

    override this.Text = $"Annotate '{this.QualifierRefExpr.ShortName}' type"

    override this.IsAvailable _ =
        isValid this.QualifierRefExpr &&

        let reference = this.QualifierRefExpr.Reference
        let mfv = reference.GetFcsSymbol().As<FSharpMemberOrFunctionOrValue>()
        isNotNull mfv &&

        let fullType = mfv.FullType
        fullType.IsGenericParameter && fullType.GenericParameter.Constraints.Count = 0
        

    new (error: IndeterminateTypeError) =
        SpecifyParameterTypeFromMemberFix(error.RefExpr)

    override this.Execute(solution, textControl) =
        let psiModule = refExpr.GetPsiModule()
        let symbolScope = getSymbolScope psiModule false

        let compiledMembers = symbolScope.GetCompiledMembers(refExpr.ShortName)
        let sourceMembers = symbolScope.GetSourceMembers(refExpr.ShortName)

        let refExpr = this.QualifierRefExpr
        let psiServices = psiModule.GetPsiServices()
        let namingManager = psiServices.Naming
        let policyProvider = namingManager.Policy.GetPolicyProvider(refExpr.Language, refExpr.GetSourceFile())
        let namingPolicy = policyProvider.GetPolicy(NamedElementKinds.Locals)
        let valueName = namingManager.Parsing.Parse(refExpr.ShortName, namingPolicy.NamingRule, policyProvider)
        let valueNameWords = valueName.GetRoot().Words |> Seq.map (fun word -> word.Text.ToLowerInvariant()) |> HashSet

        let openedModuleScopes = OpenedModulesProvider(refExpr.FSharpFile).OpenedModuleScopes

        let typeElements = 
            seq { yield! compiledMembers; yield! sourceMembers }
            |> Seq.filter (fun typeMember ->
                not typeMember.IsStatic &&
                typeMember.GetAccessRights() = AccessRights.PUBLIC &&
                
                let overridableMember = typeMember.As<IOverridableMember>()
                (isNull overridableMember || not overridableMember.IsExplicitImplementation)
            )
            |> Seq.map (fun typeMember ->
                let weight = 1.0
                let m = typeMember.As<IOverridableMember>()
                let weight = if isNotNull m && m.HasImmediateSuperMembers() then weight + 0.1 else weight
                let weight = if typeMember.IsOverride then weight + 0.1 else weight
                typeMember.ContainingType, weight)
            |> Seq.filter (fun (typeElement, _) ->
                let accessRightsOwner = typeElement.As<IAccessRightsOwner>()
                isNotNull accessRightsOwner && accessRightsOwner.GetAccessRights() = AccessRights.PUBLIC
            )
            |> List.ofSeq
            |> List.sortByDescending snd
            |> List.distinctBy fst
            |> List.map (fun (typeElement, memberWeight) ->
                let sourceName = typeElement.GetSourceName()
                let policyProvider = namingManager.Policy.GetPolicyProvider(refExpr.Language, refExpr.GetSourceFile())
                let namingPolicy = policyProvider.GetPolicy(typeElement)
                let name = namingManager.Parsing.Parse(sourceName, namingPolicy.NamingRule, policyProvider)
                let nameRoot = name.GetRoot()

                let matchingWords = 
                    nameRoot.Words
                    |> Seq.filter (fun word -> valueNameWords.Contains(word.Text.ToLowerInvariant()))
                    |> List.ofSeq

                let isImported =
                    openedModuleScopes.ContainsKey((typeElement.GetContainingNamespace()).QualifiedName)

                let weight = if typeElement :? IInterface then memberWeight - 0.1 else memberWeight
                let weight = if isImported then weight else weight + 0.1

                let matchedWeight = float matchingWords.Length / float valueNameWords.Count
                let extra = float matchingWords.Length / float nameRoot.Words.Count
                let weight = weight - matchedWeight + (1.0 - extra) / 2.0
                weight, typeElement
            )
            |> List.sortBy fst

        if typeElements.IsEmpty then
            Shell.Instance.GetComponent<ITooltipManager>().ShowAtCaret(Lifetime.Eternal, "No matching types", textControl, solution.Locks)
        else
            typeElement <- this.SelectType(typeElements, solution, textControl)
            if isNotNull typeElement then
                base.Execute(solution, textControl)

    member x.SelectType(typeElements: (float * ITypeElement) list, solution: ISolution, textControl: ITextControl) =
        let iconManager = solution.GetComponent<PsiIconManager>()
        let occurrences =
            typeElements
            |> List.map (fun (_, typeElement) ->
                let sourceName = typeElement.GetSourceName()
                let richText = RichText(sourceName)
                let icon = iconManager.GetImage(typeElement, refExpr.Language, true)
                WorkflowPopupMenuOccurrence(richText, RichText.Empty, typeElement, icon))
            |> List.toArray

        let occurrence =
            let popupMenu = solution.GetComponent<WorkflowPopupMenu>()
            popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)

        occurrence
        |> Option.ofObj
        |> Option.bind (fun occurrence -> occurrence.Entities |> Seq.tryHead)
        |> Option.toObj
