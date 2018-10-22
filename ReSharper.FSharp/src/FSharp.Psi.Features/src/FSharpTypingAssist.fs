namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features

open System
open JetBrains.Application.UI.ActionSystem.Text
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.TypingAssist
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.TextControl
open JetBrains.Util

[<SolutionComponent>]
type FSharpTypingAssist
        (lifetime, solution, settingsStore, cachingLexerService, commandProcessor, psiServices,
         externalIntellisenseHost, manager: ITypingAssistManager) as this =
    inherit TypingAssistLanguageBase<FSharpLanguage>
            (solution, settingsStore, cachingLexerService, commandProcessor, psiServices, externalIntellisenseHost)

    let [<Literal>] newLineString = "\n";

    let isAvailable = Predicate<_>(this.IsAvailable)

    let handleEnter (context: IActionContext): bool =
        let textControl = context.TextControl
        let caretOffset = textControl.Caret.Offset()
        let document = textControl.Document
        let documentBuffer = document.Buffer

        let startOffset = document.GetLineStartOffset(document.GetCoordsByOffset(caretOffset).Line)
        let mutable pos = startOffset

        while pos < caretOffset && Char.IsWhiteSpace(documentBuffer.[pos]) do
            pos <- pos + 1

        let indent = newLineString + documentBuffer.GetText(TextRange(startOffset, pos))
        let result =
            this.PsiServices.Transactions.DocumentTransactionManager.DoTransaction("Indent on Enter", fun _ ->
                document.InsertText(caretOffset, indent)
                true)

        if not result then false else

        textControl.Caret.MoveTo(caretOffset + indent.Length, CaretVisualPlacement.DontScrollIfVisible)
        true

    do
        manager.AddActionHandler(lifetime, TextControlActions.ENTER_ACTION_ID, this, Func<_,_>(handleEnter), isAvailable)

    member x.IsAvailable(context) = x.IsActionHandlerAvailabile(context)

    override x.IsSupported(textControl: ITextControl) =
        match textControl.Document.GetPsiSourceFile(x.Solution) with
        | null -> false
        | sourceFile ->

        sourceFile.IsValid() &&
        sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>() &&
        sourceFile.Properties.ProvidesCodeModel

    interface ITypingHandler with
        member x.QuickCheckAvailability(textControl, sourceFile) =
            sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>()
