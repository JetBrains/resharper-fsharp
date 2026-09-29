namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open System.Runtime.InteropServices
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpMsBuildUtils
open JetBrains.ReSharper.Psi

[<ProjectFileType(typeof<FSharpProjectFileType>)>]
type FSharpProjectFileLanguageService(projectFileType, fsFileService: IFSharpFileService) =
    inherit ProjectFileLanguageService(projectFileType)

    override x.Icon = ProjectModelThemedIcons.Fsharp.Id
    override x.PsiLanguageType = FSharpLanguage.Instance :> _

    override x.GetMixedLexerFactory(solution, _, [<Optional; DefaultParameterValue(null: IPsiSourceFile)>] sourceFile) =
        match sourceFile with
        | null -> FSharpLanguage.Instance.LanguageService().GetPrimaryLexerFactory()
        | _ ->

        let defines = solution.GetComponent<IFcsProjectProvider>().GetParsingOptions(sourceFile).ConditionalDefines
        FSharpPreprocessedLexerFactory(defines) :> _

    override x.GetPsiProperties(projectFile, sourceFile, isCompileService) =
        let providesCodeModel =
            // todo: use items container instead
            isCompileService.IsCompile(projectFile, sourceFile) ||
            fsFileService.IsScriptLike(sourceFile) ||

            let buildAction = projectFile.Properties.GetBuildAction(sourceFile.PsiModule.TargetFrameworkId)
            buildAction = BuildActions.compileBefore || buildAction = BuildActions.compileAfter
        FSharpPsiProperties(projectFile, sourceFile, providesCodeModel) :> _


[<ProjectFileType(typeof<FSharpSignatureProjectFileType>)>]
type FSharpSignatureProjectFileLanguageService(projectFileType, fsFileService) =
    inherit FSharpProjectFileLanguageService(projectFileType, fsFileService)

    override this.Icon = ProjectModelThemedIcons.FsharpSignature.Id


[<ProjectFileType(typeof<FSharpScriptProjectFileType>)>]
type FSharpScriptProjectFileLanguageService(projectFileType, fsFileService) =
    inherit FSharpProjectFileLanguageService(projectFileType, fsFileService)

    override this.Icon = ProjectModelThemedIcons.FsharpScript.Id
