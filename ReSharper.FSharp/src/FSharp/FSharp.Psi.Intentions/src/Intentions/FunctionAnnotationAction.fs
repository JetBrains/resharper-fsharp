namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Parts
open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.BulbActions
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.Util

module SpecifyTypes =
    type Availability = {
        CanSpecifyParameterTypes: bool
        CanSpecifyReturnType: bool
    } with
        member x.IsAvailable = x <> Availability.Unavailable
        static member Unavailable = { CanSpecifyParameterTypes = false; CanSpecifyReturnType = false }
        static member ParameterTypesOnly = { CanSpecifyParameterTypes = true; CanSpecifyReturnType = false }
        static member ReturnTypeOnly = { CanSpecifyParameterTypes = false; CanSpecifyReturnType = true }

    let rec private (|TupleLikePattern|_|) (pattern: IFSharpPattern) =
        match pattern with
        | :? ITuplePat as pat -> Some(pat)
        | :? IAsPat as pat ->
            match pat.LeftPattern.IgnoreInnerParens() with
            | TupleLikePattern pat -> Some(pat)
            | _ -> None
        | _ -> None

    let countParametersWithoutAnnotation (parametersOwner: IParameterOwnerMemberDeclaration) =
        let rec isAnnotated isTopLevel (pattern: IFSharpPattern) =
            let pattern = pattern.IgnoreInnerParens()
            match pattern with
            | :? ITypedPat | :? IUnitPat -> true
            | :? IAttribPat as attribPat -> isAnnotated isTopLevel attribPat.Pattern
            | TupleLikePattern pat when isTopLevel -> pat.PatternsEnumerable |> Seq.forall (isAnnotated false)
            | _ -> false

        parametersOwner.ParametersDeclarations
        |> Seq.sumBy (fun p -> if isAnnotated true p.Pattern then 0 else 1)

    let areParametersAnnotated (parametersOwner: IParameterOwnerMemberDeclaration) =
        countParametersWithoutAnnotation parametersOwner = 0

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

        let pattern =
            match pattern.IgnoreInnerParens() with
            | :? IAttribPat as attribPat -> attribPat.Pattern
            | _ -> pattern

        let pattern, fcsType =
            match pattern.IgnoreInnerParens() with
            | :? IOptionalValPat ->
                let fcsType = if isOption fcsType then fcsType.GenericArguments[0] else fcsType
                pattern, fcsType

            | _ ->

            let optionalValPat = OptionalValPatNavigator.GetByPattern(pattern)
            if isNull optionalValPat then pattern, fcsType
            else (optionalValPat : IFSharpPattern), fcsType.GenericArguments[0]

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
        let rec specifyParameterTypes (fcsParamGroups: 'a seq) (getFcsType: 'a -> FSharpType) (enumerate: 'a -> 'a seq) (parameters: IFSharpPattern seq) isTopLevel =
            for fcsParamGroup, parameter in Seq.zip fcsParamGroups parameters do
                match parameter.IgnoreInnerParens() with
                | :? IConstPat | :? ITypedPat -> ()
                | TupleLikePattern pat when isTopLevel ->
                    let fcsTypes = enumerate fcsParamGroup
                    specifyParameterTypes fcsTypes getFcsType enumerate pat.Patterns false
                | pattern ->
                    let fcsType = getFcsType fcsParamGroup
                    specifyPatternType displayContext fcsType pattern

        let parameters = decl.ParametersDeclarations |> Seq.map _.Pattern

        if mfv.IsMember then
            let enumerate x = x |> Seq.map (fun x -> [|x|] :> IList<_>)
            let types = mfv.CurriedParameterGroups
            specifyParameterTypes types (fun x -> x[0].Type) enumerate parameters true
        else
            let types = getFunctionTypeArgs true mfv.FullType
            specifyParameterTypes types id (_.GenericArguments) parameters true

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

module SpecifyTypesActionHelper =
    open SpecifyTypes

    let executePsiTransaction (node: ITreeNode) (availability: Availability) =
        use writeCookie = WriteLockCookie.Create(node.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        specifyTypes node availability


[<AbstractClass>]
type AnnotationActionBase<'a when 'a: not struct and 'a :> ITreeNode>(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    abstract member IsAvailable: 'a -> bool

    member x.ContextNode = dataProvider.GetSelectedElement<'a>()

    override x.IsAvailable(_: IUserDataHolder) =
        let node = x.ContextNode

        isValid (node :> ITreeNode) &&
        x.IsAvailable(node) &&
        SpecifyTypes.getAvailability node |> _.IsAvailable

    override x.ExecutePsiTransaction _ =
        let node = x.ContextNode
        let availability = SpecifyTypes.getAvailability node
        SpecifyTypesActionHelper.executePsiTransaction node availability

[<ContextAction(Name = "AnnotateMemberOrFunction", GroupType = typeof<FSharpContextActions>,
                Description = "Specify parameter types and the return type for the binding or type member")>]
type MemberAndFunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit AnnotationActionBase<IParameterOwnerMemberDeclaration>(dataProvider)

    override this.IsAvailable(node: IParameterOwnerMemberDeclaration) =
        isAtParametersOwnerKeywordOrIdentifier dataProvider node

    override this.Text =
        if this.ContextNode.ParametersDeclarationsEnumerable.Any() then "Add type annotations"
        else "Add type annotation"

type private SpecifyTypeAction(node: ITreeNode, availability: SpecifyTypes.Availability, ?text: string) =
    inherit ModernBulbActionBase()

    override this.Text = defaultArg text "Add type annotation"

    override this.ExecutePsiTransaction(_, _) =
        SpecifyTypesActionHelper.executePsiTransaction node availability
        null

[<ContextAction(Name = "AnnotatePattern", GroupType = typeof<FSharpContextActions>,
                Description = "Annotate named pattern type")>]
type PatternAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    static let submenuAnchor =
        SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable)
    let myActions = ResizeArray<IBulbAction>(2)

    interface IContextAction with
        override x.IsAvailable(_: IUserDataHolder) =
            let refPat = dataProvider.GetSelectedElement<IReferencePat>()

            if not (isValid refPat) then false else
            if isNotNull (BindingNavigator.GetByHeadPattern(refPat.IgnoreParentParens())) then false else

            let availability = SpecifyTypes.getAvailability refPat
            if not availability.IsAvailable then false else

            myActions.Add(SpecifyTypeAction(refPat, availability))

            let parametersOwner = ParameterOwnerMemberDeclarationNavigator.GetByParameterPattern(refPat)
            if isNull parametersOwner then true else

            let scopedActionIsAvailable = SpecifyTypes.countParametersWithoutAnnotation parametersOwner > 1
            if not scopedActionIsAvailable then true else

            myActions.Add(SpecifyTypeAction(parametersOwner, SpecifyTypes.Availability.ParameterTypesOnly, "Annotate all parameters"))
            true

        override this.CreateBulbItems() =
            let introduceSubmenu = myActions.Count > 1
            let customAnchor = if introduceSubmenu then submenuAnchor else null
            myActions.ToContextActionIntentions(customAnchor)


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
