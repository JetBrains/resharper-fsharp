namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.Symbols
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText

module SpecifyParameterBaseTypeFix =
    let rec getPatternReference (isInstPat: IFSharpPattern) =
        let asPat = AsPatNavigator.GetByLeftPattern(isInstPat)
        if isNotNull asPat then getPatternReference asPat else

        let expr = FSharpPatternUtil.ParentTraversal.tryFindSourceExpr isInstPat
        expr.As<IReferenceExpr>()


type SpecifyParameterBaseTypeFix(refExpr: IReferenceExpr, typeUsage: ITypeUsage) =
    inherit FSharpQuickFixBase()

    let pat =
        if not refExpr.IsSimpleName then null else

          refExpr.Reference.Resolve().DeclaredElement.As<ILocalReferencePat>()

    let mutable baseType: FSharpType option = None

    let getFcsEntity (typeUsage: ITypeUsage) =
        let namedTypeUsage = typeUsage.As<INamedTypeUsage>()
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

                    pat.CheckerService.ResolveNameAtLocation(pat, ["obj"], false, "SpecifyParameterBaseTypeFix.getSuperTypes")
                    |> List.tryHead
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

    new (error: IndeterminateTypeRuntimeCoercionPatternError) =
        let patternReferenceExpr = SpecifyParameterBaseTypeFix.getPatternReference error.IsInstPat
        SpecifyParameterBaseTypeFix(patternReferenceExpr, error.IsInstPat.TypeUsage)

    new (error: IndeterminateTypeRuntimeCoercionExpressionError) =
        let refExpr = error.TypeTestExpr.Expression.As<IReferenceExpr>()
        SpecifyParameterBaseTypeFix(refExpr, error.TypeTestExpr.TypeUsage)


    override this.Text =
        $"Annotate '{pat.SourceName}' type"

    override this.IsAvailable _ =
        isNotNull pat &&

        let pat, _ = FSharpPatternUtil.ParentTraversal.makePatPath pat
        isNotNull (ParametersPatternDeclarationNavigator.GetByPattern(pat)) &&

        getFcsEntity typeUsage
        |> Option.map (fun (fcsEntity, _) ->
            isNotNull fcsEntity && fcsEntity.GenericParameters.Count = 0 &&
            (fcsEntity.IsClass || fcsEntity.IsInterface) &&

            let types = getSuperTypes fcsEntity
            not types.IsEmpty
        )
        |> Option.defaultValue false

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(pat.IsPhysical())

        TypeAnnotationUtil.specifyPatternType baseType.Value pat

    override this.Execute(solution, textControl) =
        let fcsEntity, displayContext = getFcsEntity typeUsage |> Option.get
        let superTypes = getSuperTypes fcsEntity
        baseType <- this.SelectType(superTypes, displayContext, solution, textControl)

        if baseType.IsSome then
            base.Execute(solution, textControl)

    member x.SelectType(typeNames: (FSharpType * bool) list, displayContext: FSharpDisplayContext, solution: ISolution,
            textControl: ITextControl) =
        let occurrences =
            typeNames
            |> List.map (fun (fcsType, isImmediateSuperType) ->
                let icon =
                    let mapType = fcsType.MapType(refExpr).As<IDeclaredType>()
                    if isNull mapType then null else

                    let typeElement = mapType.GetTypeElement()
                    if isNull typeElement then null else

                    let iconManager = solution.GetComponent<PsiIconManager>()
                    iconManager.GetImage(typeElement, refExpr.Language, true)

                let richText = RichText(fcsType.Format(displayContext))
                if isImmediateSuperType then
                    richText.SetStyle(JetFontStyles.Bold, 0, richText.Length) |> ignore
                WorkflowPopupMenuOccurrence(richText, RichText.Empty, fcsType, icon))
            |> List.toArray

        let occurrence =
            let popupMenu = solution.GetComponent<WorkflowPopupMenu>()
            popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)

        occurrence
        |> Option.ofObj
        |> Option.bind (fun occurrence -> occurrence.Entities |> Seq.tryHead)
