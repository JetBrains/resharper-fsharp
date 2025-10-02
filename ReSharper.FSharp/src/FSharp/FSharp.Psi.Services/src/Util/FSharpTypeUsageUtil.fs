[<RequireQualifiedAccess>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpTypeUsageUtil

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

let updateTypeUsage (fcsType: FSharpType) (typeUsage: ITypeUsage) =
    let factory = typeUsage.CreateElementFactory()
    let context =
        match typeUsage with
        | :? IParameterSignatureTypeUsage -> TypeUsageContext.ParameterSignature
        | _ -> TypeUsageContext.TopLevel

    let newTypeUsage = factory.CreateTypeUsage(fcsType.Format(), context)
    let newTypeUsage = ModificationUtil.ReplaceChild(typeUsage, newTypeUsage)
    let typeUsage =
        if RedundantParenTypeUsageAnalyzer.needsParens newTypeUsage newTypeUsage then
            let parenTypeUsage = factory.CreateTypeUsage("(int)", TypeUsageContext.TopLevel) :?> IParenTypeUsage
            parenTypeUsage.SetInnerTypeUsage(newTypeUsage) |> ignore
            ModificationUtil.ReplaceChild(newTypeUsage, parenTypeUsage): ITypeUsage
        else
            newTypeUsage

    TypeAnnotationUtil.bindAnnotations [ fcsType, typeUsage ]

let setParametersOwnerReturnTypeNoBind (decl: IFSharpTypeOwnerDeclaration) (fcsType: FSharpType) =
    let factory = decl.CreateElementFactory()
    let typeUsage = factory.CreateTypeUsage(fcsType.Format(), TypeUsageContext.TopLevel)
    fcsType, decl.SetTypeUsage(typeUsage)

let setFcsParametersOwnerReturnTypeNoBind (decl: IFSharpTypeOwnerDeclaration) (mfv: FSharpMemberOrFunctionOrValue) =
    let fcsReturnType =
        let fullType = mfv.FullType
        match decl with
        | :? IBinding as binding when fullType.IsFunctionType ->
            skipFunctionParameters fullType binding.ParametersDeclarations.Count
        | _ ->
            mfv.ReturnParameter.Type

    setParametersOwnerReturnTypeNoBind decl fcsReturnType

let setFcsParametersOwnerReturnType (decl: IFSharpTypeOwnerDeclaration) =
    let mfv = decl.GetFcsSymbolUse().Symbol.As<FSharpMemberOrFunctionOrValue>()
    let fcsReturnType, returnTypeUsage = setFcsParametersOwnerReturnTypeNoBind decl mfv
    TypeAnnotationUtil.bindAnnotations [ fcsReturnType, returnTypeUsage ]


let rec skipParameters paramsToSkipCount (typeUsage: ITypeUsage) =
    if paramsToSkipCount = 0 then typeUsage else

    let funTypeUsage = typeUsage.As<IFunctionTypeUsage>()
    if isNull funTypeUsage then null else

    skipParameters (paramsToSkipCount - 1) funTypeUsage.ReturnTypeUsage


let getTupleParentNavigationPath (expr: IFSharpExpression) =
    let rec loop (expr: IFSharpExpression) acc =
        let expr = expr.IgnoreParentParens()

        let tupleExpr = TupleExprNavigator.GetByExpression(expr)
        if isNotNull tupleExpr then
            let itemIndex = tupleExpr.ExpressionsEnumerable.IndexOf(expr)
            loop tupleExpr (itemIndex :: acc)
        else
            expr, List.rev acc

    loop expr []

let rec navigateTuplePath path (typeUsage: ITypeUsage) : ITypeUsage =
    match typeUsage.IgnoreInnerParens(), path with
    | :? ITupleTypeUsage as tupleTypeUsage, step :: rest when tupleTypeUsage.ItemsEnumerable.Count() > step ->
        navigateTuplePath rest tupleTypeUsage.Items[step]

    | _, _ :: _ -> null

    | typeUsage, _ -> typeUsage.IgnoreParentParens()


[<Language(typeof<FSharpLanguage>)>]
type FSharpTypeAnnotationUtil() =
    interface IFSharpTypeAnnotationUtil with
        member this.SetPatternFcsType(pattern, fcsType) =
            TypeAnnotationUtil.specifyPatternType fcsType pattern

        member this.SetTypeOwnerFcsType(typeUsageOwnerNode, fcsType) =
            TypeAnnotationUtil.setTypeOwnerType fcsType typeUsageOwnerNode

        member this.ReplaceWithFcsType(typeUsage, fcsType) =
            updateTypeUsage fcsType typeUsage

        member this.SetPatternTypeUsage(pattern, typeUsage) =
            TypeAnnotationUtil.setPatternTypeUsage pattern typeUsage
