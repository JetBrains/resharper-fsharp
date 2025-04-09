namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.Application.Parts
open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
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
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Feature.Services.Util

module SpecifyTypes =
    type Availability = {
        CanSpecifyParameterTypes: bool
        CanSpecifyReturnType: bool
    } with
        member x.IsAvailable = x <> Availability.Unavailable
        static member Unavailable = { CanSpecifyParameterTypes = false; CanSpecifyReturnType = false }
        static member ReturnTypeOnly = { CanSpecifyParameterTypes = false; CanSpecifyReturnType = true }

    let rec private (|TupleLikePattern|_|) (pattern: IFSharpPattern) =
        match pattern with
        | :? ITuplePat as pat -> Some(pat)
        | :? IAsPat as pat ->
            match pat.LeftPattern.IgnoreInnerParens() with
            | TupleLikePattern pat -> Some(pat)
            | _ -> None
        | _ -> None

    let private areParametersAnnotated (binding: IParameterOwnerMemberDeclaration) =
        let rec isAnnotated isTopLevel (pattern: IFSharpPattern) =
            let pattern = pattern.IgnoreInnerParens()
            match pattern with
            | :? ITypedPat | :? IUnitPat -> true
            | TupleLikePattern pat when isTopLevel -> pat.PatternsEnumerable |> Seq.forall (isAnnotated false)
            | _ -> false

        binding.ParametersDeclarations |> Seq.forall (fun p -> isAnnotated true p.Pattern)

    let rec getAvailability (node: ITreeNode) =
        if not (isValid node) then Availability.Unavailable else

        match node with
        | :? IBinding as binding ->
            if binding.IsComputed then Availability.Unavailable else

            { CanSpecifyParameterTypes = not (areParametersAnnotated binding)
              CanSpecifyReturnType = isNull binding.ReturnTypeInfo }

        | :? IFSharpPattern as pattern ->
            let binding = BindingNavigator.GetByHeadPattern(pattern.IgnoreParentParens())
            if isNotNull binding then getAvailability binding else

            let pattern =
                match OptionalValPatNavigator.GetByPattern(pattern) with
                | null -> pattern
                | x -> x

            { Availability.Unavailable with
                CanSpecifyReturnType = isNotNull pattern && isNull (TypedPatNavigator.GetByPattern(pattern)) }

        | :? IMemberDeclaration as memberDeclaration ->
            { CanSpecifyReturnType =
                isNull memberDeclaration.ReturnTypeInfo &&
                Seq.isEmpty memberDeclaration.AccessorDeclarationsEnumerable

              CanSpecifyParameterTypes = not (areParametersAnnotated memberDeclaration) }

        | _ -> Availability.Unavailable

    let private specifyParametersOwnerReturnType
                    (declaration: IParameterOwnerMemberDeclaration)
                    (mfv: FSharpMemberOrFunctionOrValue)
                    displayContext =
        let typeString =
            let fullType = mfv.FullType
            if declaration :? IBinding && fullType.IsFunctionType then
                let specifiedTypesCount = declaration.ParametersDeclarations.Count

                let types = getFunctionTypeArgs true fullType
                if types.Length <= specifiedTypesCount then mfv.ReturnParameter.Type.Format(displayContext) else

                let remainingTypes = types |> List.skip specifiedTypesCount
                remainingTypes
                |> List.map (fun fcsType ->
                    let typeString = fcsType.Format(displayContext)
                    if fcsType.IsFunctionType then sprintf "(%s)" typeString else typeString)
                |> String.concat " -> "
            else
                mfv.ReturnParameter.Type.Format(displayContext)

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

    let specifyMemberReturnType (decl: IMemberDeclaration) mfv displayContext =
        Assertion.Assert(isNull decl.ReturnTypeInfo, "isNull decl.ReturnTypeInfo")
        Assertion.Assert(decl.AccessorDeclarationsEnumerable.IsEmpty(), "decl.AccessorDeclarationsEnumerable.IsEmpty()")

        specifyParametersOwnerReturnType decl mfv displayContext

    let private addParens (factory: IFSharpElementFactory) (pattern: IFSharpPattern) =
        let parenPat = factory.CreateParenPat()
        parenPat.SetPattern(pattern) |> ignore
        parenPat :> IFSharpPattern

    let specifyPatternType displayContext (fcsType: FSharpType) (pattern: IFSharpPattern) =
        let pattern = pattern.IgnoreParentParens()
        let factory = pattern.CreateElementFactory()

        let newPattern =
            match pattern.IgnoreInnerParens() with
            | :? ITuplePat as tuplePat -> addParens factory tuplePat
            | pattern -> pattern

        let typedPat =
            let typeUsage = factory.CreateTypeUsage(fcsType.Format(displayContext), TypeUsageContext.TopLevel)
            factory.CreateTypedPat(newPattern, typeUsage)

        let listConsParenPat = getOutermostListConstPat pattern |> _.IgnoreParentParens()

        ModificationUtil.ReplaceChild(pattern, typedPat)
        |> ParenPatUtil.addParensIfNeeded
        |> ignore

        // In the case `x :: _: Type` add parens to the whole listConsPat
        //TODO: improve parens analyzer
        if isNotNull listConsParenPat && listConsParenPat :? IListConsPat then
            ParenPatUtil.addParens listConsParenPat |> ignore

    let private specifyParameterTypes (decl: IParameterOwnerMemberDeclaration) (mfv: FSharpMemberOrFunctionOrValue) displayContext =
        let types = getFunctionTypeArgs true mfv.FullType
        let parameters = decl.ParametersDeclarations |> Seq.map _.Pattern

        let rec specifyParameterTypes (types: FSharpType seq) (parameters: IFSharpPattern seq) isTopLevel =
            for fcsType, parameter in Seq.zip types parameters do
                match parameter.IgnoreInnerParens() with
                | :? IConstPat | :? ITypedPat -> ()
                | TupleLikePattern pat when isTopLevel ->
                    specifyParameterTypes fcsType.GenericArguments pat.Patterns false
                | pattern ->
                    specifyPatternType displayContext fcsType pattern

        specifyParameterTypes types parameters true

    let rec specifyTypes (node: ITreeNode) (availability: Availability) =
        match node with
        | :? IFSharpPattern as pattern ->
            let binding = BindingNavigator.GetByHeadPattern(pattern.IgnoreParentParens())
            if isNotNull binding then specifyTypes binding availability else

            match pattern with
            | :? IReferencePat as pattern ->
                let binding = BindingNavigator.GetByHeadPattern(pattern)
                if isNotNull binding then specifyTypes binding availability else

                let symbolUse = pattern.GetFcsSymbolUse()

                let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
                let fcsType = mfv.FullType
                let displayContext = symbolUse.DisplayContext

                specifyPatternType displayContext fcsType pattern

            | pattern ->
                let patType = pattern.TryGetFcsType()
                let displayContext = pattern.TryGetFcsDisplayContext()

                specifyPatternType displayContext patType pattern

        | :? IParameterOwnerMemberDeclaration as declaration ->
            let symbolUse = declaration.GetFcsSymbolUse()
            if isNull symbolUse then () else

            let symbol = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
            let displayContext = symbolUse.DisplayContext

            if availability.CanSpecifyParameterTypes then
                specifyParameterTypes declaration symbol displayContext

            if availability.CanSpecifyReturnType then
                specifyParametersOwnerReturnType declaration symbol displayContext

        | _ -> ()


[<ContextAction(Name = "AnnotateFunction", GroupType = typeof<FSharpContextActions>,
                Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override x.Text = "Add type annotations"

    override x.IsAvailable _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        if isNull binding then false else
        if not (isAtBindingKeywordOrReferencePattern dataProvider binding) then false else
        SpecifyTypes.getAvailability binding |> _.IsAvailable

    override x.ExecutePsiTransaction _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let availability = SpecifyTypes.getAvailability binding
        SpecifyTypes.specifyTypes binding availability


type private SpecifyTypeAction(node: ITreeNode, availability: SpecifyTypes.Availability) =
    inherit BulbActionBase()

    override this.Text = "Add type annotation"

    override this.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(node.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        SpecifyTypes.specifyTypes node availability
        null

[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type SpecifyTypeActionsProvider(solution) =
    interface ISpecifyTypeActionProvider with
        member this.TryCreateSpecifyTypeAction(node: ITreeNode) =
            use _ = ReadLockCookie.Create()
            let availability = { SpecifyTypes.getAvailability node with CanSpecifyParameterTypes = false }
            if not availability.IsAvailable then null else

            SpecifyTypeAction(node, availability)
                .ToContextActionIntention(BulbMenuAnchors.FirstClassContextItems)
                .ToBulbMenuItem(solution, TextControlUtils.GetTextControl(node))
