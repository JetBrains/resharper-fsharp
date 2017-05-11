using JetBrains.Application.CommandProcessing;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.ActionSystem.Text;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Feature.Services.FSharp.TypingAssist
{
  [SolutionComponent]
  public class FSharpTypingAssist : TypingAssistLanguageBase<FSharpLanguage>, ITypingHandler
  {
    private const string NewLineString = "\n";

    public FSharpTypingAssist(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore,
      CachingLexerService cachingLexerService, ICommandProcessor commandProcessor, IPsiServices psiServices,
      IExternalIntellisenseHost externalIntellisenseHost, ITypingAssistManager typingAssistManager)
      : base(solution, settingsStore, cachingLexerService, commandProcessor, psiServices, externalIntellisenseHost)
    {
      typingAssistManager.AddActionHandler(lifetime, TextControlActions.ENTER_ACTION_ID, this, HandleEnterPressed,
        IsActionHandlerAvailabile);
    }

    protected override bool IsSupported(ITextControl textControl)
    {
      var projectFile = textControl.Document.GetPsiSourceFile(Solution);
      if (projectFile == null || !projectFile.IsValid())
        return false;

      var languageType = projectFile.LanguageType;
      if (!languageType.Is<FSharpProjectFileType>() && !languageType.Is<FSharpScriptProjectFileType>())
        return false;

      return projectFile.Properties.ShouldBuildPsi;
    }

    private bool HandleEnterPressed(IActionContext context)
    {
      var textControl = context.TextControl;
      var caretOffset = textControl.Caret.Offset();
      var document = textControl.Document;
      var documentBuffer = document.Buffer;
      var startOffset = document.GetLineStartOffset(document.GetCoordsByOffset(caretOffset).Line);

      var pos = startOffset;
      while (pos < caretOffset)
      {
        if (!char.IsWhiteSpace(documentBuffer[pos]))
          break;

        pos++;
      }

      var indent = NewLineString + documentBuffer.GetText(new TextRange(startOffset, pos));
      var result = Solution.GetPsiServices()
        .Transactions.DocumentTransactionManager.DoTransaction(
          "Indent on enter", () =>
          {
            textControl.Document.InsertText(caretOffset, indent);
            return true;
          });

      if (!result) return false;
      textControl.Caret.MoveTo(caretOffset + indent.Length, CaretVisualPlacement.DontScrollIfVisible);
      return true;
    }

    public bool QuickCheckAvailability(ITextControl textControl, IPsiSourceFile projectFile)
    {
      var projectFileLanguageType = projectFile.LanguageType;
      return projectFileLanguageType.Is<FSharpProjectFileType>() ||
             projectFileLanguageType.Is<FSharpScriptProjectFileType>();
    }
  }
}