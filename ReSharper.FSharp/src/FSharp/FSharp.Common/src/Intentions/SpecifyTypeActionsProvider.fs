namespace JetBrains.ReSharper.Plugins.FSharp.Intentions

open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.TextControl

[<Interface>]
type ISpecifyTypeActionsProvider =
    abstract member GetAvailableActions: textControl: ITextControl -> IntentionAction array
