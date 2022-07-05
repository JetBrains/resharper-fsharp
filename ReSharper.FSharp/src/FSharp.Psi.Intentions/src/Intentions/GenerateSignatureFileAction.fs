namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions

open System.IO
open JetBrains.Application.UI.ActionsRevised.Menu
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

type GenerateSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    interface IExecutableAction with
        member this.Update(context, presentation, nextUpdate) =
            let sourceFile = dataProvider.SourceFile.Name
            let fsiFile = Path.ChangeExtension(sourceFile, ".fsi")
            not (File.Exists fsiFile)
            
        member this.Execute(context, nextExecute) =
            let sourceFile = dataProvider.SourceFile.Name
            let fsiFile = Path.ChangeExtension(sourceFile, ".fsi")
            let currentFSharpFile = dataProvider.PsiFile
            let fcsService = currentFSharpFile.FcsCheckerService
            let checkResult = fcsService.ParseAndCheckFile(currentFSharpFile.GetSourceFile(), "for signature file", true)
            match checkResult with
            | None -> ()
            | Some { CheckResults = checkResult } ->
            
            match checkResult.GenerateSignature() with
            | None -> ()
            | Some signatureSourceText ->
                let content = string signatureSourceText
                File.WriteAllText(fsiFile, content)