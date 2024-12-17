namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.Symbols
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

module SpecifyTypes =
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

        let factory = binding.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(typeString, TypeUsageContext.TopLevel)

        let parameters = binding.ParametersDeclarations
        let anchor =
            if parameters.IsEmpty then binding.HeadPattern :> ITreeNode
            else parameters.Last() :> _

        let returnTypeInfo = ModificationUtil.AddChildAfter(anchor, factory.CreateReturnTypeInfo(typeUsage))

        if parameters.Count > 0 then
            ModificationUtil.AddChildBefore(returnTypeInfo, Whitespace()) |> ignore

    let private addParens (factory: IFSharpElementFactory) (pattern: IFSharpPattern) =
        let parenPat = factory.CreateParenPat()
        parenPat.SetPattern(pattern) |> ignore
        parenPat :> IFSharpPattern

    let specifyPattern displayContext (fcsType: FSharpType) (pattern: IFSharpPattern) =
        let pattern, fcsType =
            match pattern with
            | :? IReferencePat as pattern -> fixIfOptionalParameter pattern fcsType
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

    let specifyPropertyType displayContext (fcsType: FSharpType) (decl: IMemberDeclaration) =
        Assertion.Assert(isNull decl.ReturnTypeInfo, "isNull decl.ReturnTypeInfo")
        Assertion.Assert(decl.ParametersDeclarationsEnumerable.IsEmpty(),
            "decl.ParametersDeclarationsEnumerable.IsEmpty()")

        let factory = decl.CreateElementFactory()
        let typeUsage = factory.CreateTypeUsage(fcsType.Format(displayContext), TypeUsageContext.TopLevel)
        let returnTypeInfo = factory.CreateReturnTypeInfo(typeUsage)
        ModificationUtil.AddChildAfter(decl.Identifier, returnTypeInfo) |> ignore

[<ContextAction(Name = "AnnotateFunction", GroupType = typeof<FSharpContextActions>,
                Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let rec (|TupleLikePattern|_|) (pattern: IFSharpPattern) =
        match pattern with
        | :? ITuplePat as pat -> Some(pat)
        | :? IAsPat as pat ->
            match pat.LeftPattern.IgnoreInnerParens() with
            | TupleLikePattern pat -> Some(pat)
            | _ -> None
        | _ -> None

    let specifyParameterTypes displayContext (binding: IBinding) (mfv: FSharpMemberOrFunctionOrValue) =
        let types = FcsTypeUtil.getFunctionTypeArgs true mfv.FullType
        let parameters = binding.ParametersDeclarations |> Seq.map _.Pattern

        let rec specifyParameterTypes (types: FSharpType seq) (parameters: IFSharpPattern seq) isTopLevel =
            for fcsType, parameter in Seq.zip types parameters do
                match parameter.IgnoreInnerParens() with
                | :? IConstPat | :? ITypedPat -> ()
                | TupleLikePattern pat when isTopLevel ->
                    specifyParameterTypes fcsType.GenericArguments pat.Patterns false
                | pattern ->
                    SpecifyTypes.specifyPattern displayContext fcsType pattern

        specifyParameterTypes types parameters true


    let isAnnotated (binding: IBinding) =
        let rec isAnnotated isTopLevel (pattern: IFSharpPattern) =
            let pattern = pattern.IgnoreInnerParens()
            match pattern with
            | :? ITypedPat | :? IUnitPat -> true
            | TupleLikePattern pat when isTopLevel -> pat.PatternsEnumerable |> Seq.forall (isAnnotated false)
            | _ -> false

        isNotNull binding.ReturnTypeInfo &&
        binding.ParametersDeclarations |> Seq.forall (fun p -> isAnnotated true p.Pattern)

    let hasBangInBindingKeyword (binding: IBinding) =
        let letExpr = LetOrUseExprNavigator.GetByBinding(binding)
        if isNull letExpr then false else
        letExpr.IsComputed

    override x.Text = "Add type annotations"

    override x.IsAvailable _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        if isNull binding then false else
        if hasBangInBindingKeyword binding then false else
        isAtBindingKeywordOrReferencePattern dataProvider binding && not (isAnnotated binding)

    override x.ExecutePsiTransaction _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()

        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let refPat = binding.HeadPattern.As<IReferencePat>()
        if isNull refPat then () else

        let symbolUse = refPat.GetFcsSymbolUse()
        if isNull symbolUse then () else

        let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let displayContext = symbolUse.DisplayContext

        if binding.HasParameters then
            specifyParameterTypes displayContext binding mfv

        if isNull binding.ReturnTypeInfo then
            SpecifyTypes.specifyBindingReturnType displayContext mfv binding


[<ContextAction(Name = "AnnotatePattern", GroupType = typeof<FSharpContextActions>,
                Description = "Annotate named pattern")>]
type PatternAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)
    override x.Text = "Add type annotation"

    override x.IsAvailable _ =
        let pattern = dataProvider.GetSelectedElement<IReferencePat>().IgnoreParentParens()
        isNotNull pattern &&
        isNull (TypedPatNavigator.GetByPattern(pattern)) &&
        isNull (BindingNavigator.GetByHeadPattern(pattern))

    override x.ExecutePsiTransaction _ =
        let pattern = dataProvider.GetSelectedElement<IReferencePat>()

        use writeCookie = WriteLockCookie.Create(pattern.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let symbolUse = pattern.GetFcsSymbolUse()

        let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
        let displayContext = symbolUse.DisplayContext

        SpecifyTypes.specifyPattern displayContext mfv.FullType pattern
