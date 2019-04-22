namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open JetBrains.ReSharper.Daemon.Specific.InheritedGutterMark
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FSharpInheritedMembersHighlighterStageProcessFactory() =
    inherit InheritedMembersHighlighterProcessFactory()

    override x.CreateProcess(daemonProcess, file) =
        null
