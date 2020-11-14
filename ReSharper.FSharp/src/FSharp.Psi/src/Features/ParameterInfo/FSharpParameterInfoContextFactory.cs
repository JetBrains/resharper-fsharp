using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ParameterInfo;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ParameterInfo
{
  [ParameterInfoContextFactory(typeof(FSharpLanguage))]
  public class FSharpParameterInfoContextFactory : IParameterInfoContextFactory
  {
    private const string OpName = "FSharpParameterInfoContextFactory";
    private static readonly char[] ourPopupChars = {'(', ',', '<'};

    public bool ShouldPopup(DocumentOffset caretOffset, char c, ISolution solution,
      IContextBoundSettingsStore settingsStore)
    {
      return ourPopupChars.Contains(c);
    }

    public IParameterInfoContext CreateContext(ISolution solution, DocumentOffset caretOffset,
      DocumentOffset expectedLParenOffset, char invocationChar, IContextBoundSettingsStore settingsStore)
    {
      var fsFile = solution.GetPsiServices().GetPsiFile<FSharpLanguage>(caretOffset) as IFSharpFile;
      var parseResults = fsFile?.ParseResults?.Value;
      if (parseResults == null)
        return null;

      var document = caretOffset.Document;
      var coords = document.GetCoordsByOffset(caretOffset.Offset);
      var paramInfoLocationsOption = parseResults.FindNoteworthyParamInfoLocations(coords.ToPos());
      if (paramInfoLocationsOption == null)
        return null;

      var checkResults = fsFile.GetParseAndCheckResults(true, OpName)?.Value.CheckResults;
      if (checkResults == null)
        return null;

      var paramInfoLocations = paramInfoLocationsOption.Value;
      var names = paramInfoLocations.LongId;
      var lidEnd = paramInfoLocations.LongIdEndLocation;

      var overloads = checkResults.GetMethods(lidEnd.Line, lidEnd.Column, string.Empty, names);

      // do not show when no overloads are found or got an operator info
      // github.com/Microsoft/visualfsharp/blob/Visual-Studio-2017/vsintegration/src/FSharp.LanguageService/Intellisense.fs#L274
      if (overloads.Methods.IsEmpty() || overloads.MethodName.EndsWith("> )"))
        return null;

      var currentParamNumber = GetParameterNumber(paramInfoLocations, caretOffset);
      var rangeStartOffset = document.GetOffset(paramInfoLocations.OpenParenLocation);
      var textRange = new TextRange(rangeStartOffset, document.GetLineEndOffsetNoLineBreak(coords.Line));

      var rangeStartText = document.GetText(new TextRange(rangeStartOffset, rangeStartOffset + 1));
      var namedArgs = paramInfoLocations.NamedParamNames.TakeWhile(n => n != null).Select(n => n.Value).AsArray();
      var candidates = CreateCandidates(rangeStartText, overloads, paramInfoLocations.TupleEndLocations.Length);

      return new FSharpParameterInfoContext(currentParamNumber, candidates, textRange, namedArgs);
    }

    private static ICandidate[] CreateCandidates([NotNull] string rangeStartText, FSharpMethodGroup overloads,
      int argsCount)
    {
      var methods = overloads.Methods;
      switch (rangeStartText)
      {
        case "<":
          return methods.Select(m => new FSharpTypeArgumentCandidate(overloads.MethodName, m)).ToArray<ICandidate>();
        default:
          return methods
            .Select(m => new FSharpParameterInfoCandidate(m, argsCount > 1 && m.Parameters.Length < argsCount))
            .ToArray<ICandidate>();
      }
    }

    private static int GetParameterNumber(FSharpNoteworthyParamInfoLocations paramInfoLocations,
      DocumentOffset documentCaretOffset)
    {
      var tupleEndLocations = paramInfoLocations.TupleEndLocations;
      var offset = documentCaretOffset.Offset;
      var document = documentCaretOffset.Document;

      for (var i = 0; i < tupleEndLocations.Length; i++)
        if (document.GetOffset(tupleEndLocations[i]) > offset)
          return i;
      return 0;
    }

    public bool IsIntellisenseEnabled(ISolution solution, IContextBoundSettingsStore settingsStore)
    {
      return true; // todo: settings
    }

    public PsiLanguageType Language => FSharpLanguage.Instance;
    public IEnumerable<char> ImportantChars => new[] {'(', ')', ',', '<', '>'};
  }
}
