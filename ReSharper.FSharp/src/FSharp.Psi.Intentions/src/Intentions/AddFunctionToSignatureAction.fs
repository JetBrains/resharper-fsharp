namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.Util

[<ContextAction(Name = "AddFunctionToSignatureFile", Group = "F#", Description = "Add function to signature file")>]
type AddFunctionToSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    let isInSignatureFile (fsu: FSharpSymbolUse) =
        match fsu.Symbol.SignatureLocation with
        | None -> false
        | Some r -> r.FileName.EndsWith(".fsi")

    let getSymbol (): FSharpSymbolUse option =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        if isNull letBindings then None else

        let bindings = letBindings.Bindings
        if bindings.Count <> 1 then None else
        
        let headPat = bindings.First().HeadPattern.As<IReferencePat>()
        
        if isNull headPat then None else
            
        let functionName = headPat.Identifier
        let currentFSharpFile = dataProvider.PsiFile
        let fcsService = currentFSharpFile.FcsCheckerService
        fcsService.ResolveNameAtLocation(functionName, [| functionName.Name |], false, functionName.Name)
    
    override x.Text = "Add to signature file"
    override this.IsAvailable _ =
        let hasSignature = true // TODO: Check if the current file has an implementation file?
        if not hasSignature then false else
        let symbol = getSymbol ()
        match symbol with
        | None -> false
        | Some symbol ->
            not (isInSignatureFile symbol)
        
    override this.ExecutePsiTransaction(solution, progress) =
        let symbol = getSymbol ()
        match symbol with
        | None -> Action<_>(ignore)
        | Some symbol ->
            match symbol.Symbol with
            | :? FSharpMemberOrFunctionOrValue as f when f.IsFunction ->
                let functionName = f.DisplayName
                let arguments =
                    f.CurriedParameterGroups
                    |> Seq.map (fun p ->
                        let first = Seq.head p
                        match first.Name with
                        | None -> first.Type.QualifiedBaseName
                        | Some name -> $"name : {first.Type.QualifiedBaseName}")
                    |> String.concat " -> "
                let returnType = f.ReturnParameter.Type.QualifiedBaseName
                
                let signatureEntry = $"val {functionName} : {arguments} -> {returnType}"
                printfn "something like: %s", signatureEntry
                Action<_>(fun textControl -> ())
            | _ -> Action<_>(ignore)
        