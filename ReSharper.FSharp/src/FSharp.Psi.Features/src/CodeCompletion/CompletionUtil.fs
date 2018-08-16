[<AutoOpen>]
module rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.CompletionUtil

open System.Drawing
open JetBrains.Application.Threading
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.TextControl
open JetBrains.UI.RichText

let (|BasicCompletion|SmartCompletion|ImportCompletion|) completionType =
    if completionType == CodeCompletionType.BasicCompletion then BasicCompletion else
    if completionType == CodeCompletionType.SmartCompletion then SmartCompletion else
    if completionType == CodeCompletionType.ImportCompletion then ImportCompletion else
    failwithf "Unexpected completion type %O" completionType

let itemInfoTextStyle = TextStyle.FromForeColor(SystemColors.GrayText)

type ITextControl with
    member x.RescheduleCompletion(solution: ISolution) =
        solution.Locks.QueueReadLock("Next code completion", fun _ ->
            let language = FSharpLanguage.Instance
            let sessionManager = solution.GetComponent<ICodeCompletionSessionManager>()
            sessionManager.ExecuteAutomaticCompletionAsync(x, language, AutopopupType.HardAutopopup))
