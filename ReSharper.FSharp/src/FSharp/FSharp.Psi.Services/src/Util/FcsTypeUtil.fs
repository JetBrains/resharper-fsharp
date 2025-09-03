module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

let rec skipFunctionParameters (fcsType: FSharpType) paramsToSkipCount =
    if paramsToSkipCount = 0 || not fcsType.IsFunctionType then fcsType else

    let returnType = fcsType.GenericArguments[1]
    skipFunctionParameters returnType (paramsToSkipCount - 1)

let getFunctionTypeArgs includeReturnType fcsType =
    let rec loop (fcsType: FSharpType) acc =
        if not fcsType.IsFunctionType then [] else 

        let args = fcsType.GenericArguments
        let acc = args[0] :: acc

        let argType = args[1]
        if argType.IsFunctionType then
            loop argType acc
        else
            if includeReturnType then
                argType :: acc
            else
                acc

    loop fcsType [] |> List.rev


let private emptyDisplayContext =
    FSharpDisplayContext.Empty.WithShortTypeNames(true)


type FSharpType with
    member this.Format() =
        this.Format(emptyDisplayContext)


let rec isFcsTypeAccessible (context: ITreeNode) (fcsType: FSharpType) =
    let isTypeElementAccessible (typeElement: ITypeElement) =
        FSharpAccessRightUtil.IsAccessible(typeElement, context)

    // todo: check if IsTuple also covers IsStructTuple
    // todo: check if it's safe to just call GenericArguments without additional checks

    if fcsType.HasTypeDefinition then
        let typeElement = fcsType.TypeDefinition.GetTypeElement(context.GetPsiModule())
        isNotNull typeElement && isTypeElementAccessible typeElement &&

        (not fcsType.IsAbbreviation || isFcsTypeAccessible context fcsType.AbbreviatedType)

    elif fcsType.IsFunctionType || fcsType.IsTupleType || fcsType.IsStructTupleType || fcsType.IsAnonRecordType then
        fcsType.GenericArguments |> Seq.forall (isFcsTypeAccessible context)

    else
        true
