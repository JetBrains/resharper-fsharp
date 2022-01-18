namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.UsageChecking

open JetBrains.ReSharper.Daemon.UsageChecking
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi

type FSharpDummyUsageAnalyzer(lifetime, suppressors) =
    inherit UsageAnalyzer(lifetime, suppressors)

    override x.InteriorShouldBeProcessed(_, _) = false
    override x.ProcessElement(_, _) = ()

[<Language(typeof<FSharpLanguage>)>]
type FSharpUsageCheckingServices(lifetime, suppressors) =
    inherit UsageCheckingServices(FSharpDummyUsageAnalyzer(lifetime, suppressors), null, null)
    let _ = FSharpLanguage.Instance // workaround to create assembly reference (Microsoft/visualfsharp#3522)

    override x.CreateUnusedLocalDeclarationAnalyzer(_, _, _) = null
