namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
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
        use writeCookie = WriteLockCookie.Create(true)
        use disableFormatter = new DisableCodeFormatter()
        let letBindings = dataProvider.GetSelectedElement<ILetBindingsDeclaration>()
        let moduleDecl = ModuleDeclarationNavigator.GetByMember(letBindings)
        
        if isNotNull moduleDecl then
            let allDecls = moduleDecl.DeclaredElement.GetDeclarations() |> Seq.cast<INamedModuleDeclaration> |> Seq.toArray
            if allDecls.Length = 2 then
                let implDecl = Array.find (fun (nmd: INamedModuleDeclaration) ->
                    match nmd.Parent with
                    | :? IFSharpImplFile -> true
                    | _ -> false) allDecls
                let sigDecl = Array.find (fun (nmd: INamedModuleDeclaration) ->
                    match nmd.Parent with
                    | :? IFSharpSigFile -> true
                    | _ -> false) allDecls

                let elementFactory = sigDecl.CreateElementFactory()
                let binding = letBindings.Bindings.First()
                let refPat = binding.HeadPattern.As<IReferencePat>()
                let name = refPat.ReferenceName.Identifier.Name
                let symbolUse = refPat.GetFcsSymbolUse()
                if isNull symbolUse then () else

                let mfv = symbolUse.Symbol :?> FSharpMemberOrFunctionOrValue
                let types = FcsTypeUtil.getFunctionTypeArgs true mfv.FullType
                let parameters = binding.ParametersDeclarations
                let namedParameters =
                    Seq.zip types parameters
                    |> Seq.map (fun (t, p) ->
                        let typeName = t.TypeDefinition.DisplayName
                        match p.Pattern with
                        | :? ILocalReferencePat as pat -> $"{pat.DeclaredName}:{typeName}"
                        | _ -> typeName
                    )

                let signature =
                    match Seq.tryLast types with
                    | None -> mfv.FullType.TypeDefinition.DisplayName
                    | Some returnType ->
                        seq { yield! namedParameters; yield returnType.TypeDefinition.DisplayName}
                        |> String.concat " -> "

                let sigDeclNode : ITreeNode = elementFactory.CreateBindingSignature(name, signature)
                let lastChild = ModificationUtil.AddChildAfter(sigDecl.LastChild, NewLine(sigDeclNode.GetLineEnding()))
                let lastChild = ModificationUtil.AddChildAfter(lastChild, NewLine(sigDeclNode.GetLineEnding()))
                ModificationUtil.AddChildAfter(lastChild, sigDeclNode) |> ignore

            ()

        null
