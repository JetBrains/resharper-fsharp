namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type private ParameterToModify = {
    ParameterNameNode : ILocalReferencePat
    ParameterNodeInSignature : ISynPat
    FSharpParameters : FSharpParameter list
}

type private ParameterNodeWithType = {
    Node : ITreeNode
    FSharpParameters : FSharpParameter list
}

[<ContextAction(Name = "AnnotateFunction", Group = "F#", Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    let rec getTypeDisplayName (fsType : FSharpType) : string =
        let hasGenericArgs = fsType.HasTypeDefinition && fsType.GenericArguments |> Seq.isEmpty |> not
        let typeDef = if fsType.HasTypeDefinition then fsType.TypeDefinition.DisplayName else ""
        let genericParam = if fsType.IsGenericParameter then "'" + fsType.GenericParameter.DisplayName else ""
        let genericArgs = if hasGenericArgs then fsType.GenericArguments |> Seq.map getTypeDisplayName |> Seq.toList else List.empty
        let tupleArgs = if fsType.IsTupleType then fsType.GenericArguments |> Seq.map getTypeDisplayName |> String.concat "*" else ""
        
        [genericParam; typeDef; tupleArgs]
        |> List.append genericArgs
        |> List.filter (fun x -> String.length x <> 0)
        |> String.concat " "
        |> fun typeString -> if hasGenericArgs || fsType.IsTupleType then sprintf "(%s)" typeString else typeString
    
    override x.Text = "Annotate function with parameter types and return type"
    
    override x.IsAvailable _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        if isNull binding then false else

        match binding.HeadPattern.As<INamedPat>() with
        | null -> false
        | namedPat ->
        isNotNull namedPat.Identifier
        
    override x.ExecutePsiTransaction(_, _) =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        use _writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use _disableFormatter = new DisableCodeFormatter()
        match binding.HeadPattern.As<INamedPat>() with
        | null -> null
        | namedPat ->
            
        match box (namedPat.GetFSharpSymbol()) with
        | null -> null
        | methodSymbol ->
        let fSharpFunction =
            match methodSymbol with
            | :? FSharpMemberOrFunctionOrValue as x -> x
            | _ -> failwith "Expected function here"
            
        match namedPat.As<IParametersOwnerPat>() with
        | null -> null
        | parameterOwner ->
        let treeParameters = parameterOwner.Parameters |> Seq.toList
            
        // In the case that let statements are nested, the FSC doesn't return the display name of the parameters,
        // and so zipping the paremeters from the FSC and tree is the only way to reconcile the type information.
        let unifiedParameters =
            fSharpFunction.CurriedParameterGroups
            |> Seq.toList
            |> List.zip treeParameters
            |> List.map(fun (node, parameters) -> {Node = node; FSharpParameters = parameters |> Seq.toList})
            
        // Annotate function parameters
        let factory = namedPat.CreateElementFactory()
        let childrenToModify =
            unifiedParameters
            |> Seq.choose(fun {Node = node; FSharpParameters = curriedParams} ->
                match node with
                | :? ILocalReferencePat as ref ->
                    Some {ParameterNameNode = ref; ParameterNodeInSignature = ref; FSharpParameters = curriedParams}
                | :? IParenPat as ref ->
                    let parameterNameNode =
                        ref.Children()
                        |> Seq.choose(function | :? ILocalReferencePat as pat -> Some pat | _ -> None)
                        |> Seq.tryExactlyOne
                    parameterNameNode
                    |> Option.map(fun namedNode ->
                        {ParameterNameNode = namedNode;
                          ParameterNodeInSignature = ref;
                          FSharpParameters = curriedParams})
                | _ -> None)
            |> Seq.toList
        for ref in childrenToModify do
            let name = ref.ParameterNameNode.SourceName
            let typeName =
                match ref.FSharpParameters with
                | [] -> failwith "Unexpectedly received no type parameters"
                | [single] -> getTypeDisplayName single.Type
                | multiple -> multiple |> List.map (fun x -> getTypeDisplayName x.Type) |> String.concat "*"
            let typedPat = factory.CreateTypedPatInParens(typeName, name)
            PsiModificationUtil.replaceWithCopy ref.ParameterNodeInSignature typedPat
            
        // Annotate function return type
        if binding.ReturnTypeInfo |> isNull then
            let afterWhitespace = ModificationUtil.AddChildAfter(namedPat.LastChild, FSharpTokenType.WHITESPACE.Create(" "))
            let namedType = fSharpFunction.ReturnParameter.Type |> getTypeDisplayName |> factory.CreateReturnTypeInfo
            ModificationUtil.AddChildAfter(afterWhitespace, namedType) |> ignore

        null