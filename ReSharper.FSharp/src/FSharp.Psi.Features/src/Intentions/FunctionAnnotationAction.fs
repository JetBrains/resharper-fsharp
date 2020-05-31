namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Application.Settings

[<ContextAction(Name = "AnnotateFunction", Group = "F#", Description = "Annotate function with parameter types and return type")>]
type FunctionAnnotationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    let [<Literal>] opName = "FunctionAnnotationAction"
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
        let settingsStore = binding.FSharpFile.GetSettingsStore()
        let spaceBeforeColon = settingsStore.GetValue(fun (key : FSharpFormatSettingsKey) -> key.SpaceBeforeColon)
        
        match binding.HeadPattern.As<INamedPat>() with
        | null -> null
        | namedPat ->
        let fsharpSymbolUse = namedPat.GetFSharpSymbolUse() 
        match box(fsharpSymbolUse.Symbol) with
        | null -> null
        | methodSymbol ->
        let fSharpFunction =
            match methodSymbol with
            | :? FSharpMemberOrFunctionOrValue as x -> x
            | _ -> failwith "Expected function here"
            
        match namedPat.As<IParametersOwnerPat>() with
        | null -> null
        | parameterOwner ->
        let treeParametersWithTypes =
            parameterOwner.Parameters
            |> Seq.zip fSharpFunction.CurriedParameterGroups
            |> Seq.toList
            
        // Annotate function parameters
        let factory = namedPat.CreateElementFactory()
            
        // Replace parameters with their tooltip values - each parameter is associated with a list of FSharpType instances
        // and so `symbolUse.DisplayContext.WithShortTypeNames`, as used with return type can't be used.
        for fSharpTypes, parameter in treeParametersWithTypes do
            let paramName =
                match parameter with
                | :? ILocalReferencePat as ref -> Some ref.SourceName
                | :? IParenPat as ref ->
                    ref.Pattern.As<IReferencePat>()
                    |> Option.ofObj
                    |> Option.map(fun x -> x.SourceName)
                | _ -> None
            match paramName with
            | Some name ->
                let typeSignatures =
                    fSharpTypes
                    |> Seq.map(fun fSharpType ->
                        // If tuples aren't parenthesized then the type signature is incorrect
                        {|
                          typeString = fsharpSymbolUse.DisplayContext.WithShortTypeNames(true) |> fSharpType.Type.Format
                          needsParens = fSharpType.Type.IsTupleType
                        |})
                    |> Seq.toList
                let subTypeUsages =
                    typeSignatures
                    |> List.map(fun x ->
                        // TODO: Need to add parens to ITypeUsage
                        let str = if x.needsParens then sprintf "(%s)" x.typeString else x.typeString
                        factory.CreateTypeUsage(str))
                let typeUsage = factory.CreateTypeUsage(subTypeUsages)
                let typedPat =
                    factory.CreateTypedPat(name, typeUsage, spaceBeforeColon)
                
                let parenPat = factory.CreateParenPat()
                parenPat.SetPattern(typedPat) |> ignore
                
                PsiModificationUtil.replaceWithCopy parameter parenPat
            | None -> ()
            
        // Annotate function return type
        if binding.ReturnTypeInfo |> isNull then
            // Given the return type of a function is a single FSharpType, we don't have to use the tooltip to get its
            // pretty-printed string type
            match box(fsharpSymbolUse) with
            | null -> failwith "FSharpSymbolUse expected to be non-null"
            | symbolUseObj ->
            let symbolUse = symbolUseObj :?> FSharpSymbolUse
            let returnTypeString =
                symbolUse.DisplayContext.WithShortTypeNames(true)
                |> fSharpFunction.ReturnParameter.Type.Format
            
            let afterWhitespace = ModificationUtil.AddChildAfter(namedPat.LastChild, Whitespace(" "))
            let namedType =
                returnTypeString
                |> factory.CreateTypeUsage
                |> factory.CreateReturnTypeInfo
            ModificationUtil.AddChildAfter(afterWhitespace, namedType) |> ignore

        null