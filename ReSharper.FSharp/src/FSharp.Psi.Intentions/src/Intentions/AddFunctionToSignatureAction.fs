namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
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

    let getSignatureLocation () =
        let letBindings = dataProvider.GetSelectedElement<ILetBindings>()
        if isNull letBindings then None else

        let bindings = letBindings.Bindings
        if bindings.Count <> 1 then None else
        
        match bindings.First().HeadPattern.As<IReferencePat>() with
        | null -> None
        | pat -> pat.GetFcsSymbol().SignatureLocation
        
    override x.Text = "Add to signature file"
    override this.IsAvailable _ =
        let currentFSharpFile = dataProvider.PsiFile
        let fcsService = currentFSharpFile.FcsCheckerService
        let hasSignature = fcsService.FcsProjectProvider.HasPairFile dataProvider.SourceFile
        if not hasSignature then false else
        
        match getSignatureLocation () with
        | None -> false
        | Some range -> not (range.FileName.EndsWith(".fsi"))

    override this.ExecutePsiTransaction(solution, progress) =
        Action<_>(ignore)
        // let symbol = getSymbol ()
        // match symbol with
        // | None -> Action<_>(ignore)
        // | Some symbol ->
        //     match symbol.Symbol with
        //     | :? FSharpMemberOrFunctionOrValue as f when f.IsFunction ->
        //         let functionName = f.DisplayName
        //         let arguments =
        //             f.CurriedParameterGroups
        //             |> Seq.map (fun p ->
        //                 let first = Seq.head p
        //                 match first.Name with
        //                 | None -> first.Type.QualifiedBaseName
        //                 | Some name -> $"name : {first.Type.QualifiedBaseName}")
        //             |> String.concat " -> "
        //         let returnType = f.ReturnParameter.Type.QualifiedBaseName
        //         
        //         let signatureEntry = $"val {functionName} : {arguments} -> {returnType}"
        //         printfn "something like: %s", signatureEntry
        //         Action<_>(fun textControl -> ())
        //     | _ -> Action<_>(ignore)
        //