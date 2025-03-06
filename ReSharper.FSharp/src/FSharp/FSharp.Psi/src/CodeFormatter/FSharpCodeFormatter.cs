using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings.Calculated.Interface;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
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
  [Language(typeof(FSharpLanguage))]
  public class FSharpCodeFormatter : CodeFormatterBase<FSharpFormatSettingsKey>
  {
    private readonly ILazy<FSharpFormatterInfoProvider> myFormatterInfoProvider;

    private readonly ConcurrentDictionary<FormatterImplHelper.TokenTypePair, bool> myGluingCache = new();

    private static readonly NodeTypeSet ourInterpolatedStringStartOrMiddleParts = new(
      FSharpTokenType.REGULAR_INTERPOLATED_STRING_START, FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE,
      FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START, FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE,
      FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START, FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE);

    private static readonly NodeTypeSet ourInterpolatedStringMiddleOrEndParts = new(
      FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE, FSharpTokenType.REGULAR_INTERPOLATED_STRING_END,
      FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE, FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END,
      FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE, FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END);

    private static readonly NodeTypeSet
      ourLeftBraceStartNodes = new(FSharpTokenType.LBRACE, FSharpTokenType.LBRACE_BAR);

    private static readonly NodeTypeSet ourRightBraceEndNodes =
      new(FSharpTokenType.RBRACE, FSharpTokenType.BAR_RBRACE);

    public FSharpCodeFormatter(FSharpLanguage language, CodeFormatterRequirements requirements,
      ILazy<FSharpFormatterInfoProvider> formatterInfoProvider) : base(language, requirements)
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

      if (leftTokenType == FSharpTokenType.LINE_COMMENT)
        return MinimalSeparatorType.NewLine;

      if (myGluingCache.GetOrAdd(new FormatterImplHelper.TokenTypePair(leftTokenType, rightTokenType), AreTokensGlued))
        return leftTokenType == FSharpTokenType.LINE_COMMENT
          ? MinimalSeparatorType.NewLine
          : MinimalSeparatorType.Space;

      return MinimalSeparatorType.NotRequired;
    }

    public override ITokenNode GetMinimalSeparator(ITokenNode leftToken, ITokenNode rightToken)
    {
      if (leftToken.NodeType == FSharpTokenType.SYMBOLIC_OP && rightToken.NodeType == FSharpTokenType.IDENTIFIER)
      {
        var leftTokenParent = leftToken.Parent;

        // ^T
        var parentNodeType = leftTokenParent?.NodeType;
        if (parentNodeType == ElementType.TYPE_PARAMETER_ID ||
            parentNodeType == ElementType.TRAIT_CALL_EXPR) return null;

        // !a
        if (parentNodeType == ElementType.REFERENCE_EXPR &&
            leftTokenParent?.Parent?.NodeType == ElementType.PREFIX_APP_EXPR)
          return null;
      }

      if (leftToken.NodeType == FSharpTokenType.GREATER &&
          leftToken.Parent?.NodeType == ElementType.PREFIX_APP_TYPE_ARGUMENT_LIST)
        return null;

      return base.GetMinimalSeparator(leftToken, rightToken);
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

      Format([task], parameters);
      return new TreeRange(firstElement, lastElement);
    }

    public override void Format(FormatTask[] formatTasks, AdditionalFormatterParameters parameters = null)
    {
      parameters ??= AdditionalFormatterParameters.Empty;
      AdjustFormatTasks(formatTasks);

      var contextNode = formatTasks[0].FirstElement;
      if (contextNode == null) return;

      var formatterSettings = GetFormattingSettings(contextNode, parameters, myFormatterInfoProvider.Value);

      DoDeclarativeFormat(formatterSettings, myFormatterInfoProvider.Value, null, formatTasks,
        parameters, null, (task, _, formattingContext, progressIndicator) =>
        {
          using var subPi = progressIndicator.CreateSubProgress(1);

          FormatterImplHelper.DecoratingIterateNodes(formattingContext, new FSharpDecoratorStage(this),
            new VirtNode(formattingContext, task.FirstElement), new VirtNode(formattingContext, task.LastElement));
        });

      // DoDeclarativeFormat(formatterSettings, myFormatterInfoProvider.Value, null, formatTasks,
      //   parameters, null, null);
    }

    public override bool SupportsFormattingWithAccu => true;

    public override void FormatInsertedNodes(ITreeNode nodeFirst, ITreeNode nodeLast, bool formatSurround, bool indentSurround = false)
    {
      // Try to limit reformatting range, e.g., do go past the first token when it's the first on the line.
      // Consider this:
      //
      // do
      //     let x = y
      //     ()
      //
      // Changing `let` to `use` will also reindent the whole binding, since we're changing the beginning node,
      // resulting in
      // do
      //   use x = y
      //     ()
      //
      // We limit the range to not go past `let`/`use` on the left side.
      // The same applies for the closing node if we, e.g., replace `do` or add a comment after it.
      FormatterImplHelper.FormatInsertedNodesHelperViaTasks(this, nodeFirst, nodeLast, formatSurround, indentSurround,
        findFormattingRangeToLeft: FindFormattingRangeToLeft, findFormattingRangeToRight: FindFormattingRangeToRight);

      // FormatterImplHelper.FormatInsertedNodesHelperViaTasks(this, nodeFirst, nodeLast, formatSurround, indentSurround);
    }

    private static (ITreeNode, CodeFormatProfile) FindFormattingRangeToLeft([CanBeNull] ITreeNode node, IWhitespaceChecker checker)
    {
      if (node == null)
        return (null, CodeFormatProfile.DEFAULT);

      var seenNewLine = false;
      var currentNode = node;
      while (currentNode != null)
      {
        var prevSibling = currentNode.PrevSibling;
        if (prevSibling == null)
        {
          currentNode = currentNode.Parent;
          continue;
        }

        if (!checker.IsWhitespaceTokenOrZeroLength(prevSibling))
          return (prevSibling, seenNewLine ? CodeFormatProfile.NO_REINDENT : CodeFormatProfile.GENERATOR);

        // if ever need to remove new lines, need to update this place
        if (checker.IsNewLine(prevSibling))
          seenNewLine = true;

        currentNode = prevSibling;
      }

      return (null, CodeFormatProfile.DEFAULT);
    }

    private static (ITreeNode, CodeFormatProfile) FindFormattingRangeToRight(
      [CanBeNull] ITreeNode node, IWhitespaceChecker checker)
    {
      if (node == null)
        return (null, CodeFormatProfile.DEFAULT);

      var seenNewLine = false;
      var currentNode = node;
      while (currentNode != null)
      {
        var nextSibling = currentNode.NextSibling;
        if (nextSibling == null)
        {
          currentNode = currentNode.Parent;
          continue;
        }

        if (!checker.IsWhitespaceTokenOrZeroLength(nextSibling))
          return (nextSibling, seenNewLine ? CodeFormatProfile.NO_REINDENT : CodeFormatProfile.GENERATOR);

        if (checker.IsNewLine(nextSibling))
          seenNewLine = true;

        currentNode = nextSibling;
      }

      return (null, CodeFormatProfile.DEFAULT);
    }


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
      if (ourInterpolatedStringStartOrMiddleParts[key.Type1])
        return ourLeftBraceStartNodes[key.Type2];

      if (ourInterpolatedStringMiddleOrEndParts[key.Type2])
        return ourRightBraceEndNodes[key.Type1];

      var lexer = new FSharpLexer(new StringBuffer(key.Type1.GetSampleText() + key.Type2.GetSampleText()));
      return lexer.LookaheadToken(1) == null;
    }

    protected override Dictionary<CodeFormatProfile, ConditionalSettingsChange[]> GetSettingsToChangeForProfileImpl() =>
      new()
      {
        {
          CodeFormatProfile.NO_REINDENT,
          [
            new ConditionalSettingsChange
            {
              SettingsToChange = new List<(IScalarSetting, object)>().AddWithValue(this, x => x.RestrictedFormat, true)
            }
          ]
        }
      };
  }

  internal class FSharpDecoratorStage(IWhitespaceChecker whitespaceChecker) : TreeNodeVisitor, IDecoratingStage
  {
    public void Decorate(ITreeNode node)
    {
      Interruption.Current.CheckAndThrow();

      if (node is IMatchClause { Bar: null, FirstChild: { } firstChild } matchClause)
      {
        if (GetMatchHeaderEnd(matchClause) is { } headerEnd &&
            FormatterImplHelper.ContainsLineBreak(headerEnd, matchClause, whitespaceChecker))
          ModificationUtil.AddChildBefore(firstChild, FSharpTokenType.BAR.CreateLeafElement());
      }
    }

    private static ITreeNode GetMatchHeaderEnd(IMatchClause matchClause)
    {
      if (MatchExprNavigator.GetByClause(matchClause) is { } matchExpr)
        return matchExpr.WithKeyword;

      if (MatchLambdaExprNavigator.GetByClause(matchClause) is { } matchLambdaExpr)
        return matchLambdaExpr.FunctionKeyword;

      return null;
    }
  }
}
