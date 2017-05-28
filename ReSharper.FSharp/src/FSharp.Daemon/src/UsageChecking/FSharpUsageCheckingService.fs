namespace JetBrains.ReSharper.Plugins.FSharp.Daemon

open JetBrains.ReSharper.Daemon.UsageChecking
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.FSharp

[<Language(typeof<FSharpLanguage>)>]
type FSharpUsageCheckingServices(lifetime, suppressors) =
    inherit UsageCheckingServices(lifetime, suppressors)
    
    override x.CreateUnusedLocalDeclarationAnalyzer(_,_) = null
