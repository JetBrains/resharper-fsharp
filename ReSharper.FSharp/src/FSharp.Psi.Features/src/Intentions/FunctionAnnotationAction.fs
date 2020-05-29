namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Layout
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp

type private ParameterToModify = {
    ParameterNameNode : ILocalReferencePat
    ParameterNodeInSignature : ISynPat
}

[<ContextAction(Name = "AnnotateFunction", Group = "F#", Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    let [<Literal>] opName = "FunctionAnnotationAction"
    
    let getParameterTooltip (document: IDocument) (checkResults : FSharpCheckFileResults) pattern : string Option =
        let offset = pattern.ParameterNameNode.GetTreeEndOffset().Offset
        let coords = document.GetCoordsByOffset(offset)
        let text = document.GetLineText(coords.Line)
        let getTooltip =
            checkResults.GetStructuredToolTipText(int coords.Line + 1, int coords.Column, text, [pattern.ParameterNameNode.SourceName], FSharpTokenTag.Identifier)
        let (FSharpToolTipText layouts) = getTooltip.RunAsTask()
        
        let layout = layouts |> List.exactlyOne
        match layout with
        | FSharpStructuredToolTipElement.None
        | FSharpStructuredToolTipElement.CompositionError _ -> failwith "Expected Group tooltip element"
        | FSharpStructuredToolTipElement.Group(overloads) ->
        let overload = overloads |> Seq.exactlyOne
        
        // Tooltips in this case are prepended with "val " which we don't want in the source code - in the case that
        // a type can't be determined, it doesn't start with val, so don't try to annotate type
        let tooltipString = showL overload.MainDescription
        
        if tooltipString.IndexOf("val ", 0) = 0 then
            (showL overload.MainDescription |> Seq.skip 4 |> Seq.map string) |> String.concat "" |> Some
        else
            None
    override x.Text = "Annotate function with parameter types and return type"
    
    override x.IsAvailable _ =
        let binding = dataProvider.GetSelectedElement<IBinding>()
        if isNull binding then false else

        match binding.HeadPattern.As<IParametersOwnerPat>() with
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
            
        // Annotate function parameters
        let factory = namedPat.CreateElementFactory()
        let childrenToModify =
            treeParameters
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
            
        // Parse FSharp file, in order to get tooltips
        let parsedFile =
            namedPat.FSharpFile.GetParseAndCheckResults(true, opName)
            |> Option.defaultWith(fun () -> failwith "Unable to parse FSharp file")
        let checkResults = parsedFile.CheckResults
        let document = namedPat.FSharpFile.GetSourceFile().Document
            
        // Replace parameters with their tooltip values - each parameter is associated with a list of FSharpType instances
        // and so `symbolUse.DisplayContext.WithShortTypeNames`, as used with return type can't be used.
        childrenToModify
        |> List.map(fun ref ->
            getParameterTooltip document checkResults ref
            |> Option.map (fun signature ->
                let typedPat = factory.CreateTypedPatInParens(signature)
                fun () -> PsiModificationUtil.replaceWithCopy ref.ParameterNodeInSignature typedPat)
            |> Option.defaultValue(fun () -> ()))
        |> List.iter(fun x -> x()) // Only modify the document once the tooltips have been determined
            
        // Annotate function return type
        if binding.ReturnTypeInfo |> isNull then
            // Given the return type of a function is a single FSharpType, we don't have to use the tooltip to get its
            // pretty-printed string type
            match box(namedPat.GetFSharpSymbolUse()) with
            | null -> failwith "FSharpSymbolUse expected to be non-null"
            | symbolUseObj ->
            let symbolUse = symbolUseObj :?> FSharpSymbolUse
            let returnTypeString =
                symbolUse.DisplayContext.WithShortTypeNames(true)
                |> fSharpFunction.ReturnParameter.Type.Format
            
            let afterWhitespace = ModificationUtil.AddChildAfter(namedPat.LastChild, FSharpTokenType.WHITESPACE.Create(" "))
            let namedType = returnTypeString |> factory.CreateReturnTypeInfo
            ModificationUtil.AddChildAfter(afterWhitespace, namedType) |> ignore

        null