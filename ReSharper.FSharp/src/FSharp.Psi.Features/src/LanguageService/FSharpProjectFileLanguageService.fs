namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open System.Runtime.InteropServices
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi

[<ProjectFileType(typeof<FSharpProjectFileType>)>]
type FSharpProjectFileLanguageService(projectFileType, fsCheckerService: FSharpCheckerService, fsFileService: IFSharpFileService) =
    inherit ProjectFileLanguageService(projectFileType)

    override x.PsiLanguageType = FSharpLanguage.Instance :> _
    override x.Icon = ProjectModelThemedIcons.Fsharp.Id

    override x.GetMixedLexerFactory(_, _, [<Optional; DefaultParameterValue(null: IPsiSourceFile)>] sourceFile) =
        match sourceFile with
        | null -> FSharpLanguage.Instance.LanguageService().GetPrimaryLexerFactory()
        | _ -> FSharpLexerFactory(sourceFile, fsCheckerService.GetDefines(sourceFile)) :> _

    override x.GetPsiProperties(projectFile, sourceFile, _) =
        let providesCodeModel = projectFile.Properties.BuildAction.IsCompile() || fsFileService.IsScript(sourceFile)
        FSharpPsiProperties(projectFile, sourceFile, providesCodeModel) :> _

[<ProjectFileType(typeof<FSharpScriptProjectFileType>)>]
type FSharpScriptProjectFileLanguageService(projectFileType, fsCheckerService, fsFileService: IFSharpFileService) =
    inherit FSharpProjectFileLanguageService(projectFileType, fsCheckerService, fsFileService)
