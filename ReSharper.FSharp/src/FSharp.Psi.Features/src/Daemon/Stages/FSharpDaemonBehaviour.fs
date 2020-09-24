namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FSharpDaemonBehaviour() =
    inherit LanguageSpecificDaemonBehavior()

    override x.InitialErrorStripe(sourceFile) =
        if sourceFile.Properties.ShouldBuildPsi && sourceFile.Properties.ProvidesCodeModel &&
           sourceFile.IsLanguageSupported<FSharpLanguage>()
        then ErrorStripeRequest.STRIPE_AND_ERRORS
        else ErrorStripeRequest.NONE

    override x.RunInSolutionAnalysis = false
    override x.RunInFindCodeIssues = true
