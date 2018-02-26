[<AutoOpen>]
module rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Util

open System.Drawing
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.UI.RichText

let (|BasicCompletion|_|) completionType =
    if LanguagePrimitives.PhysicalEquality completionType CodeCompletionType.BasicCompletion then Some () else None

let (|SmartCompletion|_|) completionType =
    if LanguagePrimitives.PhysicalEquality completionType CodeCompletionType.SmartCompletion then Some () else None

let itemInfoTextStyle = TextStyle.FromForeColor(SystemColors.GrayText)
