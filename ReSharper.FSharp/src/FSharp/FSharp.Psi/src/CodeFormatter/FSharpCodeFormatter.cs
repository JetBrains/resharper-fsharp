using System.Collections.Concurrent;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Services.Formatter;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Format;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;
using JetBrains.Util.Text;
using Whitespace = JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree.Whitespace;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.CodeFormatter
{
  [Language(typeof(FSharpLanguage), InstantiationEx.DemandAnyThreadNotSafeBecauseOfCalculatedSettingsSchema)]
  public class FSharpCodeFormatter : CodeFormatterBase<FSharpFormatSettingsKey>
  {
    private readonly FSharpFormatterInfoProvider myFormatterInfoProvider;

    private readonly ConcurrentDictionary<FormatterImplHelper.TokenTypePair, bool> myGluingCache = new();

    private static readonly NodeTypeSet InterpolatedStringStartOrMiddleParts =
      new(FSharpTokenType.REGULAR_INTERPOLATED_STRING_START,
        FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE,
        FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START,
        FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE,
        FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START,
        FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE);

    private static readonly NodeTypeSet InterpolatedStringMiddleOrEndParts =
      new(FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE,
        FSharpTokenType.REGULAR_INTERPOLATED_STRING_END,
        FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE,
        FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END,
        FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE,
        FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END);

    private static readonly NodeTypeSet LeftBraceStartNodes = new(FSharpTokenType.LBRACE, FSharpTokenType.LBRACE_BAR);

    private static readonly NodeTypeSet RightBraceEndNodes = new(FSharpTokenType.RBRACE, FSharpTokenType.BAR_RBRACE);

    public FSharpCodeFormatter(FSharpLanguage language, CodeFormatterRequirements requirements,
      FSharpFormatterInfoProvider formatterInfoProvider) : base(language, requirements)
    {
      myFormatterInfoProvider = formatterInfoProvider;
    }

    protected override CodeFormattingContext CreateFormatterContext(AdditionalFormatterParameters parameters,
      ICustomFormatterInfoProvider provider, int tabWidth, SingleLangChangeAccu changeAccu, FormatTask[] formatTasks) =>
      new(this, FormatterLoggerProvider.FormatterLogger, parameters, tabWidth, changeAccu, formatTasks);

    public override MinimalSeparatorType GetMinimalSeparatorByNodeTypes(TokenNodeType leftTokenType,
      TokenNodeType rightTokenType)
    {
      if (!(leftTokenType is FSharpTokenType.FSharpTokenNodeType) ||
          !(rightTokenType is FSharpTokenType.FSharpTokenNodeType))
        return MinimalSeparatorType.NotRequired;

      if (leftTokenType.IsWhitespace || rightTokenType.IsWhitespace)
        return MinimalSeparatorType.NotRequired;

      if (myGluingCache.GetOrAdd(new FormatterImplHelper.TokenTypePair(leftTokenType, rightTokenType), AreTokensGlued))
        return leftTokenType == FSharpTokenType.LINE_COMMENT
          ? MinimalSeparatorType.NewLine
          : MinimalSeparatorType.Space;

      return MinimalSeparatorType.NotRequired;
    }

    public override bool IsNewLine(ITreeNode treeNode) => treeNode is NewLine;

    public override ITreeNode CreateSpace(string indent, NodeType replacedOrLeftSiblingType) => new Whitespace(indent);

    public override ITreeNode CreateNewLine(LineEnding lineEnding, NodeType lineBreakType = null) =>
      new NewLine(lineEnding.GetPresentation());

    public override ITreeRange Format(ITreeNode firstElement, ITreeNode lastElement, CodeFormatProfile profile,
      AdditionalFormatterParameters parameters = null)
    {
      parameters ??= AdditionalFormatterParameters.Empty;
      var task = new FormatTask(firstElement, lastElement, profile);
      task.Adjust(this);
      if (task.FirstElement == null)
        return new TreeRange(firstElement, lastElement);

      if (!firstElement.IsFSharpExperimentalFeatureEnabled(ExperimentalFeature.Formatter))
        return new TreeRange(firstElement, lastElement);

      var formatterSettings = GetFormattingSettings(task.FirstElement, parameters, myFormatterInfoProvider);

      DoDeclarativeFormat(formatterSettings, myFormatterInfoProvider, null, new[] {task},
        parameters, null, null, false);

      return new TreeRange(firstElement, lastElement);
    }

    public override void FormatInsertedNodes(ITreeNode nodeFirst, ITreeNode nodeLast, bool formatSurround, bool indentSurround = false) =>
      FormatterImplHelper.FormatInsertedNodesHelper(this, nodeFirst, nodeLast, formatSurround, indentSurround);

    public override ITreeRange FormatInsertedRange(ITreeNode nodeFirst, ITreeNode nodeLast, ITreeRange origin) =>
      FormatterImplHelper.FormatInsertedRangeHelper(this, nodeFirst, nodeLast, origin, true);

    public override void FormatReplacedNode(ITreeNode oldNode, ITreeNode newNode)
    {
      FormatInsertedNodes(newNode, newNode, true);
      FormatterImplHelper.CheckForMinimumSeparator(this, newNode);
    }

    public override void FormatReplacedRange(ITreeNode first, ITreeNode last, ITreeRange oldNodes)
    {
      FormatInsertedNodes(first, last, false);
      FormatterImplHelper.CheckForMinimumSeparator(this, first, last);
    }

    public override void FormatDeletedNodes(ITreeNode parent, ITreeNode prevNode, ITreeNode nextNode) =>
      FormatterImplHelper.FormatDeletedNodesHelper(this, parent, prevNode, nextNode,
        parent is PrimaryConstructorDeclaration);

    public override string OverridenSettingPrefix => "// @formatter:";

    private static bool AreTokensGlued(FormatterImplHelper.TokenTypePair key)
    {
      if (InterpolatedStringStartOrMiddleParts[key.Type1])
        return LeftBraceStartNodes[key.Type2];

      if (InterpolatedStringMiddleOrEndParts[key.Type2])
        return RightBraceEndNodes[key.Type1];

      var lexer = new FSharpLexer(new StringBuffer(key.Type1.GetSampleText() + key.Type2.GetSampleText()));
      return lexer.LookaheadToken(1) == null;
    }
  }
}
