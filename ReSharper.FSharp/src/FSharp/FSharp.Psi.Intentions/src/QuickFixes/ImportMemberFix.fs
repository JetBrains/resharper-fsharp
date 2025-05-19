namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Collections.Generic
open JetBrains.Application
open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.BulbActions
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell

[<AbstractClass>]
type FSharpImportMemberActionBase<'T when 'T :> IClrDeclaredElement>(reference: FSharpSymbolReference) =
    inherit ModernBulbActionBase()

    abstract Bind: unit -> unit

    override this.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(reference.GetElement().IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        this.Bind()
        null

    interface IBulbAction


type FSharpImportModuleMemberAction(typeElement: ITypeElement, reference: FSharpSymbolReference) =
    inherit FSharpImportMemberActionBase<ITypeElement>(reference)

    override this.Text =
        let typeName =
            match typeElement with
            | :? IFSharpModule as fsModule -> fsModule.QualifiedSourceName
            | typeElement -> typeElement.GetSourceName() // todo: qualified name?

        $"Import '{typeName}.{reference.GetName()}'"

    override this.Bind() =
        let referenceOwner = reference.GetElement()

        let moduleToImport = ModuleToImport.DeclaredElement(getModuleToOpenFromContainingType typeElement)
        addOpen (referenceOwner.GetDocumentStartOffset()) referenceOwner.FSharpFile moduleToImport
        reference.SetRequiredQualifiersForContainingType(typeElement, referenceOwner)


type FSharpImportExtensionMemberAction(typeMember: ITypeMember, reference) =
    inherit FSharpImportMemberActionBase<ITypeMember>(reference)

    override this.Bind() =
        let referenceOwner = reference.GetElement()
        FSharpBindUtil.bindDeclaredElementToReference referenceOwner reference typeMember "bind"

    override this.Text =
        let containingTypeShortName = typeMember.ContainingType.ShortName
        $"Use {containingTypeShortName}.{reference.GetName()}"


type FSharpImportStaticMemberFromQualifierTypeAction(typeElement: ITypeElement, reference: FSharpSymbolReference) =
    inherit FSharpImportMemberActionBase<ITypeElement>(reference)

    let [<Literal>] id = "FSharpImportStaticMemberFromQualifierTypeAction"

    override this.Text =
        let typeName = typeElement.GetQualifiedName()
        $"Import '{typeName}'"

    override this.Bind() =
        let referenceOwner = reference.GetElement()
        FSharpBindUtil.bindDeclaredElementToReference referenceOwner reference typeElement id


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


type FSharpImportExtensionMemberFix(reference: IReference) =
    inherit FSharpImportMemberFixBase<ITypeMember>(reference)

    override this.FindMembers(reference) =
        let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
        if isNull refExpr then [] else

        let qualifierExpr = refExpr.Qualifier
        if isNull qualifierExpr then [] else

        let fcsType = qualifierExpr.TryGetFcsType()
        if isNull fcsType then [] else

        let name = reference.GetName()
        FSharpExtensionMemberUtil.getExtensionMembers qualifierExpr fcsType (Some name) |> Seq.cast

    override this.CreateAction(typeMember, reference) =
        FSharpImportExtensionMemberAction(typeMember, reference)


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

        let autoOpenCache = referenceOwner.GetPsiServices().Solution.GetComponent<FSharpAutoOpenCache>()
        let openedScopes = OpenedModulesProvider(referenceOwner.FSharpFile, autoOpenCache)

        let result = HashSet()

        for typeElement in typeElements do
            Interruption.Current.CheckAndThrow()

            match typeElement with
            | :? IEnum as enum when enum.HasMemberWithName(name, false) ->
                result.Add(typeElement) |> ignore

            | _ ->

            let names =
                match typeElement with
                | :? IFSharpSourceTypeElement as fsTypeElement ->
                    match fsTypeElement with
                    | :? IFSharpModule as fsModule ->
                        match referenceContext.Value with
                        | FSharpReferenceContext.Expression ->
                            seq {
                                fsModule.ValueNames
                                fsModule.FunctionNames
                                fsModule.ActivePatternNames
                            }

                        | FSharpReferenceContext.Pattern ->
                            seq {
                                fsModule.LiteralNames
                                fsModule.ActivePatternCaseNames
                            }

                        | _ -> Seq.empty

                    | _ -> seq { typeElement.GetUnionCaseNames() }

                // todo: unify with IFSharpTypeElement
                | :? IFSharpCompiledTypeElement as fsCompiledTypeElement ->
                    seq { fsCompiledTypeElement.GetUnionCaseNames() }

                | _ -> Seq.empty

            if names |> Seq.exists (fun names -> SharedImplUtil.HasMemberWithName(names, name, false)) then
                if openedScopes.Contains(typeElement, referenceOwner) then () else

                if FSharpResolveUtil.canReference reference typeElement then
                    result.Add(typeElement) |> ignore

        result

    override this.CreateAction(typeElement, reference) =
        FSharpImportModuleMemberAction(typeElement, reference)


type FSharpImportStaticMemberFromQualifierTypeFix(reference: IReference) =
    inherit FSharpImportMemberFixBase<ITypeElement>(reference)

    override this.FindMembers(reference) =
        if not (FSharpImportStaticMemberUtil.isAvailable reference) then [] else

        let memberName = reference.GetName()
        let qualifierReference = reference.QualifierReference
        let accessContext = FSharpAccessContext(qualifierReference.GetElement())

        let result = HashSet()

        for typeElement in FSharpImportStaticMemberUtil.getTypeElements true qualifierReference do
            for typeMember in typeElement.EnumerateOwnMembersWithName(memberName, false) do
                if typeMember.IsStatic && AccessUtil.IsSymbolAccessible(typeMember, accessContext) then
                    result.Add(typeElement) |> ignore

        result

    override this.CreateAction(typeElement, fcsReference) =
        FSharpImportStaticMemberFromQualifierTypeAction(typeElement, fcsReference.QualifierReference)
