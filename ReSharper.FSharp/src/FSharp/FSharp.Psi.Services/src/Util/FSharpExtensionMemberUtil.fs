module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpExtensionMemberUtil

open System
open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application
open JetBrains.Metadata.Reader.API
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods.Queries
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Resolve.TypeInference
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.Util
open JetBrains.Util.Extension

type FSharpRequest(psiModule, exprType: IType, name: string option) =
    static let memberKinds = [ExtensionMemberKind.CLASSIC_METHOD; FSharpExtensionMemberKind.INSTANCE]

    let name = Option.toObj name

    let baseTypes: IReadOnlyList<IType> =
        if isNull exprType then [] else

        let isArray = exprType :? IArrayType
    
        let rec removeArrayType (exprType: IType) =
            match exprType with
            | :? IArrayType as arrayType -> removeArrayType arrayType.ElementType
            | _ -> exprType.As<IDeclaredType>()
    
        let exprDeclaredType = removeArrayType exprType
        if isNull exprDeclaredType then [] else

        let result = List<IType>()
        result.Add(exprType)

        for superType in exprDeclaredType.GetAllSuperTypes() do
            let superType: IType =
                if isArray then
                    TypeFactory.CreateArrayType(superType, 1, NullableAnnotation.Unknown)
                else
                    superType

            result.Add(superType)
            
        if isArray then
            let predefinedType = exprType.Module.GetPredefinedType()
            result.Add(predefinedType.Array)
            result.AddRange(predefinedType.Array.GetAllSuperTypes())

        result.AsReadOnly()

    interface IExtensionMembersRequest with
        member this.Name = name
        member this.IsCaseSensitive = true

        member this.Kinds = memberKinds

        member this.ReceiverType = exprType
        member this.PossibleReceiverTypes = baseTypes

        member this.ForModule = psiModule
        member this.ContainingNamespaces = []
        member this.ContainingTypes = []

        member this.WithName _ = failwith "todo"
        member this.WithKinds _ = failwith "todo"
        member this.WithReceiverType _ = failwith "todo"
        member this.WithModule _ = failwith "todo"
        member this.WithContainingNamespaces _ = failwith "todo"
        member this.WithContainingTypes _ = failwith "todo"

[<return: Struct>]
let (|FSharpSourceExtensionMember|_|) (typeMember: ITypeMember) =
    match typeMember with
    | :? IFSharpTypeMember as fsTypeMember ->
        match fsTypeMember.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsExtensionMember ->
            match mfv.DeclaringEntity with
            | Some fcsEntity when fcsEntity.IsFSharpModule -> ValueSome mfv
            | _ -> ValueNone
        | _ -> ValueNone
    | _ -> ValueNone

[<return: Struct>]
let (|FSharpCompiledExtensionMember|_|) (typeMember: ITypeMember) =
    match typeMember with
    | :? IMethod as method ->
        let containingType = method.ContainingType
        if containingType :? IFSharpModule && containingType :? IFSharpCompiledTypeElement then
            let typeShortName = method.ShortName.SubstringBefore(".", StringComparison.Ordinal)
            ValueSome(typeShortName)
        else
            ValueNone

    | _ -> ValueNone

[<return: Struct>]
let (|FSharpExtensionMember|_|) (typeMember: ITypeMember) =
    match typeMember with
    | FSharpSourceExtensionMember _
    | FSharpCompiledExtensionMember _ -> ValueSome()
    | _ -> ValueNone

let getExtensionMembersForType (context: ITreeNode) (fcsType: FSharpType) isStaticContext (nameOpt: string option) =
    if isNull fcsType then EmptyList.InstanceList else

    let psiModule = context.GetPsiModule()
    let solution = psiModule.GetSolution()
    use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

    let exprType = fcsType.MapType(context)

    let exprTypeElements =
        let typeElements = List()

        let exprTypeElement =
            if exprType :? IArrayType then
                exprType.Module.GetPredefinedType().Array.GetTypeElement()
            else
                exprType.GetTypeElement()

        if isNotNull exprTypeElement then
            typeElements.Add(exprTypeElement)
            typeElements.AddRange(exprTypeElement.GetSuperTypeElements())

        typeElements.AsReadOnly()

    let openedModulesProvider = OpenedModulesProvider(context)

    let matchesType (typeMember: ITypeMember) : bool =
        match typeMember with
        | FSharpSourceExtensionMember mfv ->
            let apparentEnclosingEntity = mfv.ApparentEnclosingEntity
            apparentEnclosingEntity.IsSome &&

            let extendedTypeElement = apparentEnclosingEntity.Value.GetTypeElement(typeMember.Module)
            isNotNull exprTypeElements && exprTypeElements |> Seq.exists extendedTypeElement.Equals

        | FSharpCompiledExtensionMember extendedTypeShortName ->
            let getFSharpTypeName (typeElement: ITypeElement) =
                let sourceName = typeElement.GetSourceName()
                match typeElement.TypeParametersCount with
                | 0 -> sourceName
                | count -> $"{sourceName}`{count}"

            exprTypeElements |> Seq.exists (getFSharpTypeName >> (=) extendedTypeShortName)

        | :? IMethod as method ->
            let parameters = method.Parameters
            if parameters.Count = 0 then false else

            let thisParameter = parameters[0]
            let parameterType = thisParameter.Type
            let parameterKind = thisParameter.Kind
            let typeParameters = method.TypeParameters.ToIReadOnlyList()
            let conversionRule = ClrPredefinedTypeConversionRule.INSTANCE
            if typeParameters.Count > 0 then
                let substitution = TypeInferenceUtil.InferTypes(FSharpLanguage.Instance, psiModule, exprType, parameterType, typeParameters, conversionRule)
                isNotNull substitution &&

                let substitutedType = substitution.Apply(parameterType)
                conversionRule.HasExtensionMethodThisArgumentConversion(exprType, substitutedType, parameterKind, false)
            else
                conversionRule.HasExtensionMethodThisArgumentConversion(exprType, parameterType, parameterKind, false)

        | _ -> false

    let matchesName (typeMember: ITypeMember) =
        match nameOpt with
        | None -> true
        | Some name ->

        match typeMember with
        | FSharpCompiledExtensionMember _ ->
            let memberName = typeMember.ShortName
            memberName = name ||
            memberName = $"get_{name}" ||
            memberName = $"set_{name}" ||
  
            memberName.EndsWith($".{name}") ||
            memberName.EndsWith($".get_{name}") ||
            memberName.EndsWith($".set_{name}")

        | _ -> typeMember.ShortName = name

    let resolvesAsExtensionMember (typeMember: ITypeMember) =
        match typeMember with
        | :? IFSharpDeclaredElement -> typeMember :? IFSharpMethod || typeMember :? IFSharpProperty
        | _ -> true

    let matchesCallingConvention (typeMember: ITypeMember) =
        match typeMember with
        | FSharpSourceExtensionMember mfv ->
            mfv.IsInstanceMember <> isStaticContext

        | FSharpCompiledExtensionMember _ ->
            typeMember.ShortName.EndsWith(".Static", StringComparison.Ordinal) = isStaticContext

        | _ -> not isStaticContext

    let isInScope (typeMember: ITypeMember) : bool =
        match typeMember with
        | FSharpExtensionMember -> FSharpImportUtil.areMembersInScope openedModulesProvider typeMember.ContainingType
        | _ -> FSharpImportUtil.isExtensionMemberInScope openedModulesProvider typeMember

    let isApplicable (typeMember: ITypeMember) =
        resolvesAsExtensionMember typeMember &&
        matchesName typeMember &&
        not (isInScope typeMember) &&
        FSharpAccessRightUtil.IsAccessible(typeMember.ContainingType, context) &&
        matchesCallingConvention typeMember &&
        matchesType typeMember

    let query = ExtensionMembersQuery(solution.GetPsiServices(), FSharpRequest(psiModule, exprType, nameOpt)) //.MaybeForReceiverType(exprType)

    let result = List()
    for typeMember in query.EnumerateMembers() do
        Interruption.Current.CheckAndThrow()
        if isApplicable typeMember then
            result.Add(typeMember)

    result

let getNonImportedExtensionMembers (context: ITreeNode) (nameOpt: string option) (refExpr: IReferenceExpr) : IList<ITypeMember> =
    if isNull refExpr then EmptyList.InstanceList else

    let isStaticContext = FSharpExpressionUtil.isStaticContext refExpr.Qualifier
    let fcsType = getQualifierFcsType refExpr
    getExtensionMembersForType context fcsType isStaticContext nameOpt

let groupByNameAndNs members =
    members |> Seq.groupBy (fun (typeMember: ITypeMember) ->
        Interruption.Current.CheckAndThrow()

        let name =
            typeMember.ShortName
                .SubstringBeforeLast(".Static")
                .SubstringAfterLast(".")
                .SubstringAfter("get_")
                .SubstringAfter("set_")

        let ns =
            match typeMember.ContainingType with
            | :? IFSharpModule as fsModule ->
                fsModule.QualifiedSourceName

            | containingType ->
                containingType.GetContainingNamespace().QualifiedName

        ns, name
    )
