[<AutoOpen>]
module rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.CompletionUtil

open System.Drawing
open JetBrains.Application.Threading
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.TextControl
open JetBrains.UI.RichText

let (|BasicCompletion|_|) completionType =
    if LanguagePrimitives.PhysicalEquality completionType CodeCompletionType.BasicCompletion then Some () else None

let (|SmartCompletion|_|) completionType =
    if LanguagePrimitives.PhysicalEquality completionType CodeCompletionType.SmartCompletion then Some () else None

let itemInfoTextStyle = TextStyle.FromForeColor(SystemColors.GrayText)

type ITextControl with
    member x.RescheduleCompletion(solution: ISolution) =
        solution.Locks.QueueReadLock("Next code completion", fun _ ->
            let language = FSharpLanguage.Instance
            let sessionManager = solution.GetComponent<ICodeCompletionSessionManager>()
            sessionManager.ExecuteAutomaticCompletionAsync(x, language, AutopopupType.HardAutopopup))
