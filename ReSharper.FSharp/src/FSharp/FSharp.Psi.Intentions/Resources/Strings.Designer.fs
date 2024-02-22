namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Resources

open System
open JetBrains.Application.I18n
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.Util
open JetBrains.Util.Logging
open JetBrains.Application.I18n.Plurals

[<global.System.Diagnostics.DebuggerNonUserCode>]
[<global.System.Runtime.CompilerServices.CompilerGenerated>]
type public Strings() =
    static let logger = Logger.GetLogger("JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Resources.Strings")

    static let mutable resourceManager = null

    static do
        CultureContextComponent.Instance.WhenNotNull(Lifetime.Eternal, fun lifetime instance ->
            lifetime.Bracket(
                (fun () ->
                    resourceManager <-
                        lazy
                            instance.CreateResourceManager("JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Resources.Strings",
                                typeof<Strings>.Assembly)),
                (fun () -> resourceManager <- null)
            )
        )

    [<global.System.ComponentModel.EditorBrowsable(global.System.ComponentModel.EditorBrowsableState.Advanced)>]
    static member ResourceManager: JetResourceManager =
        match resourceManager with
            | null -> ErrorJetResourceManager.Instance
            | _ -> resourceManager.Value

    static member FSharpDisableAllInspectionsInFile_Text = Strings.ResourceManager.GetString("FSharpDisableAllInspectionsInFile_Text")
    static member FSharpDisableOnceWithComment_Text = Strings.ResourceManager.GetString("FSharpDisableOnceWithComment_Text")
    static member FSharpDisableInFileWithComment_Text = Strings.ResourceManager.GetString("FSharpDisableInFileWithComment_Text")
    static member FSharpDisableAndRestoreWithComments_Text = Strings.ResourceManager.GetString("FSharpDisableAndRestoreWithComments_Text")