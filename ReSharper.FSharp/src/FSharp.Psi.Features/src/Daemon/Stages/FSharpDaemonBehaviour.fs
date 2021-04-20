namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FSharpDaemonBehaviour() =
    inherit LanguageSpecificDaemonBehavior()

    override x.InitialErrorStripe(sourceFile) =
        let properties = sourceFile.Properties

        if sourceFile.IsLanguageSupported<FSharpLanguage>() then
            if not properties.ShouldBuildPsi then
                ErrorStripeRequestWithDescription.CreateNoneNoPsi(properties)
            elif not properties.ProvidesCodeModel then
                ErrorStripeRequestWithDescription.CreateNoneNoCodeModel(properties)
            else
                ErrorStripeRequestWithDescription.StripeAndErrors

        else ErrorStripeRequestWithDescription.None("The file does not support F# language")

    override x.RunInSolutionAnalysis = false
    override x.RunInFindCodeIssues = true
