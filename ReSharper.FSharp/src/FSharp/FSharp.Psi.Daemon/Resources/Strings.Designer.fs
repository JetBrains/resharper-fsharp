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
        CultureContextComponent.Instance.Change.Advise(Lifetime.Eternal, fun (args:PropertyChangedEventArgs<CultureContextComponent>) ->
            let instance = if args.HasNew then args.New else null
            if instance <> null then
                resourceManager <- Lazy<JetResourceManager>(fun _ ->
                    instance.CreateResourceManager("DPA.Monitoring.Resources.Strings", typeof<Strings>.Assembly))
            else
                resourceManager <- null)
        
    [<global.System.ComponentModel.EditorBrowsable(global.System.ComponentModel.EditorBrowsableState.Advanced)>]
    static member ResourceManager: JetResourceManager =
        match resourceManager with
            | null -> ErrorJetResourceManager.Instance
            | _ -> resourceManager.Value

    static member FSharpInferredTypeHighlighting_ProviderId = Strings.ResourceManager.GetString("FSharpInferredTypeHighlighting_ProviderId")
    static member FSharpInferredTypeHighlighting_TooltipText = Strings.ResourceManager.GetString("FSharpInferredTypeHighlighting_TooltipText")
    static member InferredTypeCodeVisionProvider_TypeCopied_TooltipText = Strings.ResourceManager.GetString("InferredTypeCodeVisionProvider_TypeCopied_TooltipText")
