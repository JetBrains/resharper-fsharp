namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Collections.Generic
open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.BulbActions
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type FSharpImportAction(typeElement: ITypeElement, reference: FSharpSymbolReference) =
    inherit ModernBulbActionBase()

    override this.Text =
        let typeName =
            match typeElement with
            | :? IFSharpModule as fsModule -> fsModule.QualifiedSourceName
            | typeElement -> typeElement.GetSourceName()

        $"Import '{typeName}.{reference.GetName()}'"

    override this.ExecutePsiTransaction(_, _) =
        let fsReference = reference.As<FSharpSymbolReference>()
        let referenceOwner = fsReference.GetElement()
        let moduleToImport = ModuleToImport.DeclaredElement(getModuleToOpenFromContainingType typeElement)

        use writeCookie = WriteLockCookie.Create(referenceOwner.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        addOpen (referenceOwner.GetDocumentStartOffset()) referenceOwner.FSharpFile moduleToImport
        fsReference.SetRequiredQualifiersForContainingType(typeElement, referenceOwner)

        null

    interface IBulbAction

type FSharpImportModuleMemberFix(reference: IReference) =
    inherit FSharpQuickFixBase()

    let findTypes () : seq<ITypeElement> =
        let fsReference = reference.As<FSharpSymbolReference>()
        let referenceOwner = fsReference.GetElement()

        if isNull referenceOwner || fsReference.IsQualified then [] else

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
            match typeElement with
            | :? IEnum as enum when enum.HasMemberWithName(name, false) ->
                result.Add(typeElement) |> ignore

            | _ ->

            let names =
                match typeElement with
                | :? IFSharpTypeElement as fsTypeElement ->
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
                if not (openedScopes.Contains(typeElement, referenceOwner)) then
                    result.Add(typeElement) |> ignore

        result

    override this.Text = failwith "todo"

    override this.IsAvailable _ =
        findTypes ()
        |> Seq.isEmpty
        |> not

    override this.CreateBulbItems() =
        let importActions =
            findTypes ()
            |> Seq.map (fun typeElement -> FSharpImportAction(typeElement, reference :?> _) :> IBulbAction)
            |> Seq.toArray

        let anchor: IAnchor =
            if importActions.Length > 2 then
                SubmenuAnchor(ResolveProblemsFixAnchors.ImportFix, "Import...")
            else
                ResolveProblemsFixAnchors.ImportFix

        importActions.ToQuickFixIntentions(anchor)
