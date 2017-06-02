namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.UsageChecking

open JetBrains.ReSharper.Daemon.UsageChecking
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.FSharp

type FSharpDummyUsageAnalyzer(lifetime, suppressors) =
    inherit UsageAnalyzer(lifetime, suppressors)
    
    override x.InteriorShouldBeProcessed(_,_) = false
    override x.ProcessElement(_,_) = () 

[<Language(typeof<FSharpLanguage>)>]
type FSharpUsageCheckingServices(lifetime, suppressors) =
    inherit UsageCheckingServices(FSharpDummyUsageAnalyzer(lifetime, suppressors), null, null)
    
    override x.CreateUnusedLocalDeclarationAnalyzer(_,_) = null
