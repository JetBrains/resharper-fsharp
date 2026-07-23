namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Collections.Generic
open System.Linq
open JetBrains.Application
open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.ReSharper.Feature.Services.BulbActions
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Actions
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Intentions.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<Language(typeof<FSharpLanguage>)>]
type FSharpExtensionMemberImportUtil() =
    inherit ExtensionMemberImportUtilBase()

    override this.CollectApplicableCandidates(applicableCandidates, reference, acceptableErrorTypes) =
        let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
        if isNull refExpr then () else

        let name = reference.GetName()
        FSharpExtensionMemberUtil.getNonImportedExtensionMembers refExpr (Some name) refExpr
        |> FSharpExtensionMemberUtil.groupByNameAndNs
        |> Seq.map (snd >> Seq.tryHead)
        |> Seq.choose id
        |> applicableCandidates.AddRange

    override this.LanguageType = FSharpLanguage.Instance


[<AbstractClass>]
type FSharpImportMemberActionBase<'T when 'T :> IClrDeclaredElement>(reference: FSharpSymbolReference) =
    inherit ModernBulbActionBase()

    abstract Bind: unit -> unit

    override this.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(reference.GetElement().IsPhysical())
        this.Bind()
        null

    interface IBulbAction


type FSharpImportModuleMemberAction(typeElement: ITypeElement, reference: FSharpSymbolReference) =
    inherit FSharpImportMemberActionBase<ITypeElement>(reference)

    override this.Text =
        let typeName =
            match typeElement with
            | :? IFSharpModule as fsModule -> fsModule.QualifiedSourceName
            | typeElement -> typeElement.GetSourceName()

        $"Import '{typeName}.{reference.GetName()}'"

    override this.Bind() =
        let referenceOwner = reference.GetElement()

        let moduleToImport = ModuleToImport.DeclaredElement(getModuleToOpenFromContainingType typeElement)
        addOpen (referenceOwner.GetDocumentStartOffset()) referenceOwner.FSharpFile moduleToImport
        reference.SetRequiredQualifiersForContainingType(typeElement, referenceOwner)


[<AbstractClass>]
type FSharpImportMemberFixBase<'T when 'T :> IClrDeclaredElement>(reference: IReference) =
    inherit FSharpQuickFixBase()

    let reference = reference.As<FSharpSymbolReference>()

    override this.Text = failwith "todo"

    abstract FindMembers: FSharpSymbolReference -> 'T seq
    abstract CreateAction: 'T * FSharpSymbolReference -> IBulbAction

    override this.IsAvailable _ =
        isNotNull reference &&

        this.FindMembers(reference)
        |> Seq.isEmpty
        |> not

    override this.CreateBulbItems() =
        let importActions =
            this.FindMembers(reference)
            |> Seq.map (fun typeMember -> this.CreateAction(typeMember, reference))
            |> Seq.toArray

        let anchor: IAnchor =
            if importActions.Length > 2 then
                SubmenuAnchor(ResolveProblemsFixAnchors.ImportFix, "Import...")
            else
                ResolveProblemsFixAnchors.ImportFix

        importActions.ToQuickFixIntentions(anchor)


type FSharpImportModuleMemberFix(reference: IReference) =
    inherit FSharpImportMemberFixBase<ITypeElement>(reference)

    override this.FindMembers(reference) =
        let referenceOwner = reference.GetElement()
        if isNull referenceOwner || reference.IsQualified then [] else

        let referenceContext = referenceOwner.ReferenceContext
        if not referenceContext.HasValue then [] else

        let name = reference.GetName()
        let psiModule = referenceOwner.GetPsiModule()
        let symbolScope = getSymbolScope psiModule false
        let typeElements = symbolScope.GetAllTypeElementsGroupedByName()

        let openedScopes = OpenedModulesProvider(referenceOwner)

        let result = HashSet()

        for typeElement in typeElements do
            Interruption.Current.CheckAndThrow()

            let names =
                match typeElement with
                | :? IFSharpModule as fsModule ->
                    match referenceContext.Value with
                    | FSharpReferenceContext.Expression ->
                        seq {
                            fsModule.ValueNames
                            fsModule.LiteralNames
                            fsModule.FunctionNames
                            fsModule.ActivePatternNames
                        }

                    | FSharpReferenceContext.Pattern ->
                        seq {
                            fsModule.LiteralNames
                            fsModule.ActivePatternCaseNames
                        }

                    | _ -> Seq.empty

                | :? IFSharpTypeElement as fsTypeElement ->
                    fsTypeElement.GetUnionCaseNames() |> Seq.singleton

                | _ -> Seq.empty

            if names |> Seq.exists (fun names -> SharedImplUtil.HasMemberWithName(names, name, false)) then
                if openedScopes.Contains(typeElement, referenceOwner) then () else

                if FSharpResolveUtil.canReference reference typeElement then
                    result.Add(typeElement) |> ignore

        result

    override this.CreateAction(typeElement, reference) =
        FSharpImportModuleMemberAction(typeElement, reference)


type FSharpImportExtensionMemberAction(typeMember: ITypeMember, reference: FSharpSymbolReference) =
    inherit ModernBulbActionBase()

    override this.Text =
        let containingTypeShortName = typeMember.ContainingType.ShortName
        $"Use {containingTypeShortName}.{reference.GetName()}"

    override this.ExecutePsiTransaction(_, _) =
        let reference = QuickFixUtil.BindTo(reference, [|typeMember|])

        if isNull reference then BulbActionCommands.ShowTooltip("Failed to import extension member")
        else null

    interface IModernManualScopedAction with
        member this.ExecuteAction(solution, scope, sourceHighlighting, progress) =
            this.ExecutePsiTransaction(solution, progress)
    
        member this.FileCollectorInfo = FileCollectorInfo.Empty
        member this.ScopedText = this.Text

type FSharpImportExtensionMemberFix(reference: IReference) =
    inherit ScopedImportQuickFixBase(reference)

    let reference = reference.As<FSharpSymbolReference>()

    override this.CreateBulbActions() =
        let extensionMembers = this.EnumeratePossibleExtensionMembers(reference)
        [| for KeyValue(_, extensionMembers) in extensionMembers do
            for m in extensionMembers -> FSharpImportExtensionMemberAction(m, reference) |]

    override this.CreateBulbItems() =
        let importActions = this.CreateBulbActions().ToArray()

        let anchor: IAnchor =
            if importActions.Length > 2 then
                SubmenuAnchor(ResolveProblemsFixAnchors.ImportFix, "Import and use...")
            else
                ResolveProblemsFixAnchors.ImportFix

        importActions.ToQuickFixIntentions(anchor)
