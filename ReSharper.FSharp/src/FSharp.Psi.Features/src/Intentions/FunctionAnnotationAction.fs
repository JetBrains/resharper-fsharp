namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
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

    let parameterTypeDisplayName (parameter : FSharpParameter) : string =
        if parameter.Type.IsGenericParameter then
            "\'" + parameter.Type.GenericParameter.DisplayName
        else
            let genericParams =
                match parameter.Type.GenericArguments |> Seq.toList with
                | [] -> ""
                | genericArgs ->
                    (genericArgs
                     |> Seq.map(fun x -> x.TypeDefinition.DisplayName)
                     |> String.concat " ")
                    + " "
            genericParams + parameter.Type.TypeDefinition.DisplayName 
    
    override x.Text = "Annotate function with parameter types and return type"
    
    override x.IsAvailable _ =
        let letExpr = dataProvider.GetSelectedElement<ILetBindings>()
        if isNull letExpr then false else
        true
        // TODO MC: replicate logic to check composition here is correct before saying is available
    override x.ExecutePsiTransaction(_, _) =
        let letExpr = dataProvider.GetSelectedElement<ILetBindings>()
        use _writeCookie = WriteLockCookie.Create(letExpr.IsPhysical())
        use _disableFormatter = new DisableCodeFormatter()
        let binding = letExpr.Bindings |> Seq.exactlyOne
        match binding.HeadPattern.As<INamedPat>() with
        | null -> null
        | namedPat ->
            
        let methodSymbol = namedPat.GetFSharpSymbol()
        let fSharpFunction =
            match methodSymbol with
            | :? FSharpMemberOrFunctionOrValue as x -> x
            | _ -> failwith "Expected function here"
        let treeParameters =
            namedPat.Children()
            |> Seq.choose(
                function
                 | :? ILocalReferencePat as ref -> ref :> ITreeNode |> Some
                 | :? IParenPat as ref -> ref :> ITreeNode |> Some
                 | _ -> None)
            |> Seq.toList
            
        // In the case that let statements are nested, the FSC doesn't return the display name of the parameters,
        // and so zipping the paremeters from the FSC and tree is the only way to reconcile the type information.
        let unifiedParameters =
            fSharpFunction.CurriedParameterGroups
            |> Seq.toList
            |> List.zip treeParameters
            |> List.map(fun (node, parameters) -> {Node = node; FSharpParameters = parameters |> Seq.toList})
            
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
                | [single] -> parameterTypeDisplayName single
                | multiple -> multiple |> List.map (parameterTypeDisplayName) |> String.concat "*"
            let typedPat = factory.CreateTypedPatInParens(typeName, name)
            PsiModificationUtil.replaceWithCopy ref.ParameterNodeInSignature typedPat
            
        if binding.ReturnTypeInfo |> isNull then
            let afterWhitespace = ModificationUtil.AddChildAfter(namedPat.LastChild, FSharpTokenType.WHITESPACE.Create(" "))
            let namedType = fSharpFunction.ReturnParameter |> parameterTypeDisplayName |> factory.CreateReturnTypeInfo
            ModificationUtil.AddChildAfter(afterWhitespace, namedType) |> ignore

        null