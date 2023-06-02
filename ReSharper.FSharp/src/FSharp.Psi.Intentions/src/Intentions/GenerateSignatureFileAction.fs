namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions

open System.IO
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.RdBackend.Common.Features.ProjectModel
open JetBrains.RdBackend.Common.Features.ProjectModel.View.Ordering
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open type JetBrains.ReSharper.Psi.PsiSourceFileExtensions

[<ContextAction(Group = "F#", Name = "Generate signature file for current file", Priority = 1s,
                Description = "Generate signature file for current file.")>]
type GenerateSignatureFileAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)
    
    override this.Text = "Generate signature file for current file"
    
    override this.IsAvailable _ =
        let currentFSharpFile = dataProvider.PsiFile
        let fcsService = currentFSharpFile.FcsCheckerService
        let hasSignature = fcsService.FcsProjectProvider.HasPairFile dataProvider.SourceFile
        not hasSignature
        
    override this.ExecutePsiTransaction(solution, _) =
        let projectFile = dataProvider.SourceFile.ToProjectFile()
        let physicalPath = dataProvider.SourceFile.ToProjectFile().Location.FileAccessPath
        let fsiFile = Path.ChangeExtension(physicalPath, ".fsi")
        
        try
            let currentFSharpFile = dataProvider.PsiFile
            let fcsService = currentFSharpFile.FcsCheckerService
            let checkResult = fcsService.ParseAndCheckFile(currentFSharpFile.GetSourceFile(), "for signature file", true)
            do
                match checkResult with
                | None -> ()
                | Some { CheckResults = checkResult } ->
                
                match checkResult.GenerateSignature() with
                | None -> ()
                | Some signatureSourceText ->
                    let content = string signatureSourceText
                    File.WriteAllText(fsiFile, content)
        with ex ->
            // TODO: show some balloon thing?
            ()

        solution.InvokeUnderTransaction(fun transactionCookie ->
            let virtualPath = FileSystemPath.TryParse(fsiFile).ToVirtualFileSystemPath()
            let relativeTo = RelativeTo(projectFile, RelativeToType.Before)
            transactionCookie.AddFile(projectFile.ParentFolder, virtualPath, context = OrderingContext(relativeTo))
            |> ignore)

        null