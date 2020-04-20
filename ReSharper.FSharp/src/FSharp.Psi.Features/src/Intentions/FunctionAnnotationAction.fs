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
}

[<ContextAction(Name = "AnnotateFunction", Group = "F#", Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

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
        // TODO MC: Match up compiler parameters with document parameters by ordering as name isn't included for nested functions...
        let compilerParameters =
            fSharpFunction.CurriedParameterGroups
            |> Seq.map(Seq.exactlyOne)
            |> Seq.toList
            
        let factory = namedPat.CreateElementFactory()
        let childrenToModify =
            namedPat.Children()
            |> Seq.choose(
                function
                    | :? ILocalReferencePat as ref ->
                        Some {ParameterNameNode = ref; ParameterNodeInSignature = ref}
                    | :? IParenPat as ref ->
                        let parameterNameNode =
                            ref.Children()
                            |> Seq.choose(function | :? ILocalReferencePat as pat -> Some pat | _ -> None)
                            |> Seq.tryExactlyOne
                        parameterNameNode
                        |> Option.map(fun namedNode ->
                            {ParameterNameNode = namedNode;
                              ParameterNodeInSignature = ref})
                    | _ -> None)
            |> Seq.toList
        for ref in childrenToModify do
            let name = ref.ParameterNameNode.CompiledName
            let typeName =
                compilerParameters
                |> List.find(fun x -> (x.Name |> Option.get) = name)
                |> fun x -> x.Type.TypeDefinition.CompiledName
            let typedPat = factory.CreateTypedPatInParens(typeName, name)
            PsiModificationUtil.replaceWithCopy ref.ParameterNodeInSignature typedPat
            
        if binding.ReturnTypeInfo |> isNull then
            let afterWhitespace = ModificationUtil.AddChildAfter(namedPat.LastChild, FSharpTokenType.WHITESPACE.Create(" "))
            let namedType = factory.CreateReturnTypeInfo fSharpFunction.ReturnParameter.Type.TypeDefinition.CompiledName
            ModificationUtil.AddChildAfter(afterWhitespace, namedType) |> ignore

        null