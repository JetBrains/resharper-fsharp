namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Psi.Impl

type FSharpPsiProperties(projectFile, sourceFile) =
    inherit DefaultPsiProjectFileProperties(projectFile, sourceFile)

    override x.ProvidesCodeModel =
      x.ProjectFile.Properties.BuildAction.IsCompile() ||
      x.ProjectFile.LanguageType.Equals(FSharpScriptProjectFileType.Instance)

    override x.ShouldBuildPsi = true
