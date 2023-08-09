namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources

open System
open JetBrains.Application.I18n
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.Util
open JetBrains.Util.Logging

[<global.System.Diagnostics.DebuggerNonUserCode>]
[<global.System.Runtime.CompilerServices.CompilerGenerated>]
type public Strings() =
    static let logger = Logger.GetLogger("JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources.Strings")

    static let mutable resourceManager = null

    static do
        CultureContextComponent.Instance.WhenNotNull(Lifetime.Eternal, fun lifetime instance ->
            lifetime.Bracket(
                (fun () ->
                    resourceManager <-
                        lazy
                            instance.CreateResourceManager("JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources.Strings",
                                typeof<Strings>.Assembly)),
                (fun () -> resourceManager <- null)
            )
        )

    [<global.System.ComponentModel.EditorBrowsable(global.System.ComponentModel.EditorBrowsableState.Advanced)>]
    static member ResourceManager: JetResourceManager =
        match resourceManager with
            | null -> ErrorJetResourceManager.Instance
            | _ -> resourceManager.Value

    static member FSharpInferredTypeHighlighting_ProviderId = Strings.ResourceManager.GetString("FSharpInferredTypeHighlighting_ProviderId")
    static member FSharpInferredTypeHighlighting_TooltipText = Strings.ResourceManager.GetString("FSharpInferredTypeHighlighting_TooltipText")
    static member InferredTypeCodeVisionProvider_TypeCopied_TooltipText = Strings.ResourceManager.GetString("InferredTypeCodeVisionProvider_TypeCopied_TooltipText")