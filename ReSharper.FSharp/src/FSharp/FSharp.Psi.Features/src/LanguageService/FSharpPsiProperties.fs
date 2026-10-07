namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open JetBrains.ReSharper.Psi.Impl

type FSharpPsiProperties(projectFile, sourceFile, providesCodeModel) =
    inherit DefaultPsiProjectFileProperties(projectFile, sourceFile)

    override x.ProvidesCodeModel = providesCodeModel
    override x.ShouldBuildPsi = true
