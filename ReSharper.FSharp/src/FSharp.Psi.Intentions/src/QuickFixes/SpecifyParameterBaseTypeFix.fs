namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.Symbols
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText

type SpecifyParameterBaseTypeFix(error: IndeterminateTypeRuntimeCoercionError) =
    inherit FSharpQuickFixBase()

    let isInstPat = error.IsInstPat
    let mutable baseType: (FSharpType * FSharpDisplayContext) option = None

    let rec getDeclPat (isInstPat: IFSharpPattern) =
        let asPat = AsPatNavigator.GetByLeftPattern(isInstPat)
        if isNotNull asPat then getDeclPat asPat else

        let matchClause = MatchClauseNavigator.GetByPattern(isInstPat)
        let matchExpr = MatchExprNavigator.GetByClause(matchClause)
        let refExpr = matchExpr.Expression.As<IReferenceExpr>()

        let reference = refExpr.Reference
        reference.Resolve().DeclaredElement.As<ILocalReferencePat>()

    let getFcsEntity (isInstPat: IIsInstPat) =
        let namedTypeUsage = isInstPat.TypeUsage.As<INamedTypeUsage>()
        if isNull namedTypeUsage then None else

        let reference = namedTypeUsage.ReferenceName.Reference
        let symbolUse = reference.GetSymbolUse()
        if isNull symbolUse then None else

        match symbolUse.Symbol with
        | :? FSharpEntity as fcsEntity -> Some(fcsEntity, symbolUse.DisplayContext)
        | _ -> None

    let getSuperTypes (fcsEntity: FSharpEntity) =
        let rec loop (types, level) (fcsType: FSharpType) =
            let types = (fcsType, level = 1) :: types

            if not fcsType.HasTypeDefinition then types, level else

            let level = level + 1
            let acc = types, level

            let fcsEntity = fcsType.TypeDefinition
            let types, _ = fcsEntity.DeclaredInterfaces |> Seq.fold loop acc

            let acc = types, level

            let acc =
                match fcsType.BaseType with
                | Some baseType ->
                    let acc =
                        if fcsEntity.IsInterface && baseType.ErasedType.BasicQualifiedName = "System.Object" then
                            types, level + 1
                        else
                            acc

                    loop acc baseType

                | None ->
                    if not fcsEntity.IsInterface then acc else

                    isInstPat.CheckerService.ResolveNameAtLocation(isInstPat, ["obj"], false, "SpecifyParameterBaseTypeFix.getSuperTypes")
                    |> Option.map (fun symbolUse ->
                        match symbolUse.Symbol with
                        | :? FSharpEntity as fcsEntity -> loop (types, level + 1) (fcsEntity.AsType())
                        | _ -> acc
                    )
                    |> Option.defaultValue acc

            acc

        loop ([], 0) (fcsEntity.AsType())
        |> fst
        |> List.distinctBy fst
        |> List.rev
        |> List.tail

    override this.Text =
        let pat = getDeclPat isInstPat
        $"Annotate '{pat.SourceName}' type"

    override this.IsAvailable _ =
        let pat = getDeclPat isInstPat

        let pat =
            let refPat = pat.IgnoreParentParens()
            match TuplePatNavigator.GetByPattern(refPat).IgnoreParentParens() with
            | null -> refPat
            | tuplePat -> tuplePat

        isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(pat)) &&

        getFcsEntity isInstPat
        |> Option.map (fun (fcsEntity, _) ->
            isNotNull fcsEntity && fcsEntity.GenericParameters.Count = 0 &&
            (fcsEntity.IsClass || fcsEntity.IsInterface) &&

            let types = getSuperTypes fcsEntity
            not types.IsEmpty
        )
        |> Option.defaultValue false

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(isInstPat.IsPhysical())

        let pat = getDeclPat isInstPat
        let baseType, displayContext = baseType.Value
        SpecifyTypes.specifyParameterType displayContext baseType pat

    override this.Execute(solution, textControl) =
        let fcsEntity, displayContext = getFcsEntity isInstPat |> Option.get
        let superTypes = getSuperTypes fcsEntity
        baseType <- this.SelectType(superTypes, displayContext, solution, textControl)

        if baseType.IsSome then
            base.Execute(solution, textControl)

    member x.SelectType(typeNames: (FSharpType * bool) list, displayContext: FSharpDisplayContext, solution: ISolution,
            textControl: ITextControl) =
        let occurrences =
            typeNames
            |> List.map (fun (fcsType, isImmediateSuperType) ->
                let richText = RichText(fcsType.Format(displayContext))
                if isImmediateSuperType then
                    richText.SetStyle(JetFontStyles.Bold, 0, richText.Length)
                WorkflowPopupMenuOccurrence(richText, RichText.Empty, (fcsType, displayContext)))
            |> List.toArray

        let occurrence =
            let popupMenu = solution.GetComponent<WorkflowPopupMenu>()
            popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)

        occurrence
        |> Option.ofObj
        |> Option.bind (fun occurrence -> occurrence.Entities |> Seq.tryHead)
