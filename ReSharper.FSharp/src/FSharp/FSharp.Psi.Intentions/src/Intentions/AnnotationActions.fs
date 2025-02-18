namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.Application.Parts
open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.Application.UI.Controls.BulbMenu.Items
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Resources
open JetBrains.ReSharper.Plugins.FSharp.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

module SpecifyTypes =
    type Availability = {
        ParameterTypes: bool
        ReturnType: bool
    } with
        member x.IsAvailable = x <> Availability.Unavailable
        static member Unavailable = { ParameterTypes = false; ReturnType = false }
        static member ReturnTypeOnly = { ParameterTypes = false; ReturnType = true }

    let rec private (|TupleLikePattern|_|) (pattern: IFSharpPattern) =
        match pattern with
        | :? ITuplePat as pat -> Some(pat)
        | :? IAsPat as pat ->
            match pat.LeftPattern.IgnoreInnerParens() with
            | TupleLikePattern pat -> Some(pat)
            | _ -> None
        | _ -> None

    let private isParametersAnnotated (binding: IParameterOwnerMemberDeclaration) =
        let rec isAnnotated isTopLevel (pattern: IFSharpPattern) =
            let pattern = pattern.IgnoreInnerParens()
            match pattern with
            | :? ITypedPat | :? IUnitPat -> true
            | TupleLikePattern pat when isTopLevel -> pat.PatternsEnumerable |> Seq.forall (isAnnotated false)
            | _ -> false

        binding.ParametersDeclarations |> Seq.forall (fun p -> isAnnotated true p.Pattern)

    let getAvailability (node: ITreeNode) =
        if not (isValid node) then Availability.Unavailable else

        match node with
        | :? IBinding as binding ->
            { ParameterTypes = not (isParametersAnnotated binding)
              ReturnType = isNull binding.ReturnTypeInfo }

        | :? IFSharpPattern as pattern ->
            let pattern =
                match OptionalValPatNavigator.GetByPattern(pattern) with
                | null -> pattern
                | x -> x

            { Availability.Unavailable with
                ReturnType = isNotNull pattern && isNull (TypedPatNavigator.GetByPattern(pattern)) }

        | :? IMemberDeclaration as memberDeclaration ->
            { ReturnType =
                isNull memberDeclaration.ReturnTypeInfo &&
                Seq.isEmpty memberDeclaration.AccessorDeclarationsEnumerable

              ParameterTypes = not (isParametersAnnotated memberDeclaration) }

        | _ -> Availability.Unavailable

    let private specifyParametersOwnerReturnType typeString (declaration: IParameterOwnerMemberDeclaration) =
        let factory = declaration.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString, TypeUsageContext.TopLevel)

        let parameters = declaration.ParametersDeclarations
        let anchor =
            if parameters.IsEmpty then
                match declaration with
                | :? IBinding as binding -> binding.HeadPattern :> ITreeNode
                | :? IMemberDeclaration as memberDeclaration -> memberDeclaration.Identifier
                | x -> failwith $"Expected binding or member declaration, but was {x}"
            else parameters.Last() :> _

        let returnTypeInfo = ModificationUtil.AddChildAfter(anchor, factory.CreateReturnTypeInfo(typeUsage))

        if parameters.Count > 0 && declaration :? IBinding then
            ModificationUtil.AddChildBefore(returnTypeInfo, Whitespace()) |> ignore

    let private specifyMemberReturnType displayContext (fcsType: FSharpType) (decl: IMemberDeclaration) =
        Assertion.Assert(isNull decl.ReturnTypeInfo, "isNull decl.ReturnTypeInfo")
        Assertion.Assert(decl.AccessorDeclarationsEnumerable.IsEmpty(), "decl.AccessorDeclarationsEnumerable.IsEmpty()")

        specifyParametersOwnerReturnType (fcsType.Format(displayContext)) decl

    let specifyBindingReturnType displayContext (mfv: FSharpMemberOrFunctionOrValue) (binding: IBinding) =
        let typeString =
            let fullType = mfv.FullType
            if fullType.IsFunctionType then
                let specifiedTypesCount = binding.ParametersDeclarations.Count

                let types = FcsTypeUtil.getFunctionTypeArgs true fullType
                if types.Length <= specifiedTypesCount then mfv.ReturnParameter.Type.Format(displayContext) else

                let remainingTypes = types |> List.skip specifiedTypesCount
                remainingTypes
                |> List.map (fun fcsType ->
                    let typeString = fcsType.Format(displayContext)
                    if fcsType.IsFunctionType then sprintf "(%s)" typeString else typeString)
                |> String.concat " -> "
            else
                mfv.ReturnParameter.Type.Format(displayContext)

        specifyParametersOwnerReturnType typeString binding

    let private addParens (factory: IFSharpElementFactory) (pattern: IFSharpPattern) =
        let parenPat = factory.CreateParenPat()
        parenPat.SetPattern(pattern) |> ignore
        parenPat :> IFSharpPattern

    let specifyPatternType displayContext (fcsType: FSharpType) (pattern: IFSharpPattern) =
        let pattern, fcsType =
            match pattern with
            | :? IReferencePat as pattern -> FcsTypeUtil.tryGetOuterOptionalParameterAndItsType pattern fcsType
            | _ -> pattern, fcsType

        let pattern = pattern.IgnoreParentParens()
        let factory = pattern.CreateElementFactory()

        let newPattern =
            match pattern.IgnoreInnerParens() with
            | :? ITuplePat as tuplePat -> addParens factory tuplePat
            | pattern -> pattern

        let typedPat =
            let typeUsage = factory.CreateTypeUsage(fcsType.Format(displayContext), TypeUsageContext.TopLevel)
            factory.CreateTypedPat(newPattern, typeUsage)

        ModificationUtil.ReplaceChild(pattern, typedPat)
        |> ParenPatUtil.addParensIfNeeded
        |> ignore

    let private specifyMemberParameterTypes displayContext (binding: IParameterOwnerMemberDeclaration) (mfv: FSharpMemberOrFunctionOrValue) =
        let types = FcsTypeUtil.getFunctionTypeArgs true mfv.FullType
        let parameters = binding.ParametersDeclarations |> Seq.map _.Pattern

        let rec specifyParameterTypes (types: FSharpType seq) (parameters: IFSharpPattern seq) isTopLevel =
            for fcsType, parameter in Seq.zip types parameters do
                match parameter.IgnoreInnerParens() with
                | :? IConstPat | :? ITypedPat -> ()
                | TupleLikePattern pat when isTopLevel ->
                    specifyParameterTypes fcsType.GenericArguments pat.Patterns false
                | pattern ->
                    specifyPatternType displayContext fcsType pattern

        specifyParameterTypes types parameters true

    let specifyPropertyType displayContext (fcsType: FSharpType) (decl: IMemberDeclaration) =
        Assertion.Assert(decl.ParametersDeclarationsEnumerable.IsEmpty(), "decl.ParametersDeclarationsEnumerable.IsEmpty()")
        specifyMemberReturnType displayContext fcsType decl

    let rec specifyTypes
        (node: ITreeNode)
        (availability: Availability) =
        match node with
        | :? IReferencePat as pattern ->
            let binding = BindingNavigator.GetByHeadPattern(pattern)
            if isNotNull binding then specifyTypes binding availability else

            let symbolUse = pattern.GetFcsSymbolUse()

            let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
            let fcsType = mfv.FullType
            let pattern, fcsType = tryGetOuterOptionalParameterAndItsType pattern fcsType
            let displayContext = symbolUse.DisplayContext

            specifyPatternType displayContext fcsType pattern

        | :? IOptionalValPat as optionalPat ->
            specifyTypes optionalPat.Pattern availability

        | :? IFSharpPattern as pattern ->
            let patType = pattern.TryGetFcsType()
            let displayContext = pattern.TryGetFcsDisplayContext()

            specifyPatternType displayContext patType pattern

        | :? IBinding as binding ->
            let refPat = binding.HeadPattern.As<IReferencePat>()
            if isNull refPat then () else

            let symbolUse = refPat.GetFcsSymbolUse()
            if isNull symbolUse then () else

            let symbol = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
            let displayContext = symbolUse.DisplayContext

            if availability.ParameterTypes then
                specifyMemberParameterTypes displayContext binding symbol

            if availability.ReturnType then
                specifyBindingReturnType displayContext symbol binding

        | :? IMemberDeclaration as memberDeclaration ->
            let symbolUse = memberDeclaration.GetFcsSymbolUse()
            if isNull symbolUse then () else

            let symbol = symbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
            if isNull symbol then () else

            let displayContext = symbolUse.DisplayContext

            if availability.ParameterTypes then
                specifyMemberParameterTypes displayContext memberDeclaration symbol

            if availability.ReturnType then
                specifyMemberReturnType displayContext symbol.ReturnParameter.Type memberDeclaration

        | _ -> ()


[<ContextAction(Name = "AnnotateFunction", GroupType = typeof<FSharpContextActions>,
                Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let hasBangInBindingKeyword (binding: IBinding) =
        let letExpr = LetOrUseExprNavigator.GetByBinding(binding)
        if isNull letExpr then false else
        letExpr.IsComputed

    override x.Text = "Add type annotations"

    override x.IsAvailable _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        if isNull binding then false else
        if hasBangInBindingKeyword binding then false else
        if not (isAtBindingKeywordOrReferencePattern dataProvider binding) then false else
        SpecifyTypes.getAvailability binding |> _.IsAvailable

    override x.ExecutePsiTransaction _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let availability = SpecifyTypes.getAvailability binding
        SpecifyTypes.specifyTypes binding availability


[<ContextAction(Name = "AnnotatePattern", GroupType = typeof<FSharpContextActions>,
                Description = "Annotate named parameter/pattern with it is type")>]
type PatternAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)
    override x.Text = "Add type annotation"

    override x.IsAvailable _ =
        let pattern = dataProvider.GetSelectedElement<IReferencePat>().IgnoreParentParens()
        isNull (BindingNavigator.GetByHeadPattern(pattern)) &&
        SpecifyTypes.getAvailability pattern |> _.IsAvailable

    override x.ExecutePsiTransaction _ =
        let pattern = dataProvider.GetSelectedElement<IReferencePat>()

        use writeCookie = WriteLockCookie.Create(pattern.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let availability = SpecifyTypes.getAvailability pattern
        SpecifyTypes.specifyTypes pattern availability


[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type SpecifyTypeActionsProvider() =
    interface ISpecifyTypeActionProvider with
        member this.TryCreateSpecifyTypeAction(node) =
            use _ = ReadLockCookie.Create()
            let availability = { SpecifyTypes.getAvailability node with ParameterTypes = false }
            if not availability.IsAvailable then null else

            BulbMenuItem(ExecutableItem(fun _ ->
                use _ = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(node.GetPsiServices(), "Add type annotation")
                use writeCookie = WriteLockCookie.Create(node.IsPhysical())
                use disableFormatter = new DisableCodeFormatter()
                SpecifyTypes.specifyTypes node availability
                ), "Add type annotation", BulbThemedIcons.ContextAction.Id, BulbMenuAnchors.FirstClassContextItems)
