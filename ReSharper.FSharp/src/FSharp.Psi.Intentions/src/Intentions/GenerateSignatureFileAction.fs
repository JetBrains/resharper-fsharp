namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions

open System
open System.IO
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
        let project = dataProvider.SourceFile.GetProject()
        
        let physicalPath = dataProvider.SourceFile.ToProjectFile().Location.FileAccessPath
        let fsiFile = Path.ChangeExtension(physicalPath, ".fsi")        
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

        Action<_>(ignore)