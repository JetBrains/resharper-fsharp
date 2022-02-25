using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Psi.Xml.XmlDocComments;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
{
  internal class FSharpDocCommentXmlPsi : ClrDocCommentXmlPsi<XmlDocBlock>
  {
    private FSharpDocCommentXmlPsi(
      [NotNull] InjectedPsiHolderNode docCommentsHolder,
      [NotNull] XmlDocBlock fSharpDocCommentBlock,
      [NotNull] IXmlFile xmlFile, bool isShifted)
      : base(docCommentsHolder, xmlFile, isShifted, fSharpDocCommentBlock)
    {
    }

    [NotNull]
    public static FSharpDocCommentXmlPsi BuildPsi([NotNull] XmlDocBlock block)
    {
      BuildXmlPsi(
        FSharpXmlDocLanguage.Instance.NotNull(), block, GetCommentLines(block),
        out var holderNode, out var xmlPsiFile, out var isShifted);

      return new FSharpDocCommentXmlPsi(holderNode, block, xmlPsiFile, isShifted);
    }

    [NotNull, Pure]
    public static IReadOnlyList<string> GetCommentLines([NotNull] FSharpDocCommentBlock block)
    {
      return block.DocComments
        .SelectMany(CommentNodeToLines)
        .ToIReadOnlyList();
    }

    [NotNull, ItemNotNull, Pure]
    private static IEnumerable<string> CommentNodeToLines([NotNull] IFSharpCommentNode commentNode)
    {
      var commentText = commentNode.CommentText;

      if (commentNode.CommentType == CommentType.MultilineComment)
      {
        var lines = commentText.SplitByNewLine();
        if (lines.Length == 0) yield break;

        var firstLine = lines[0];
        if (!string.IsNullOrWhiteSpace(firstLine))
        {
          yield return firstLine.NotNull();
        }

        var commonIndent = GetCommonLinesIndent(lines);

        for (var index = 1; index < lines.Length; index++)
        {
          var line = lines[index];

          if (index == lines.Length - 1)
          {
            if (string.IsNullOrWhiteSpace(line)) break;
          }

          if (commonIndent != null)
          {
            Assertion.Assert(line.StartsWith(commonIndent, StringComparison.Ordinal), "indent expected");

            line = line.Substring(startIndex: commonIndent.Length);
          }

          yield return line;
        }
      }
      else
      {
        yield return commentText;
      }
    }

    [Pure, NotNull]
    private static IEnumerable<int> GetMultilineCommentLinesStartOffsets([NotNull] IFSharpCommentNode commentNode,
      bool isShifted)
    {
      var commentText = commentNode.CommentText;

      var lines = commentText.SplitByNewLine();
      if (lines.Length == 0) yield break;

      var commonIndent = GetCommonLinesIndent(lines);
      var lineIndex = 0;

      foreach (var lineRange in StringUtil.SplitByNewLineRanges(commentText))
      {
        var commentBodyOffset = lineRange.StartOffset + "/**".Length;
        var line = lines[lineIndex++];

        if (lineIndex == 1)
        {
          if (string.IsNullOrWhiteSpace(line)) continue;
        }
        else if (lineIndex == lines.Length && string.IsNullOrWhiteSpace(line))
        {
          break;
        }
        else if (commonIndent != null)
        {
          Assertion.Assert(line.StartsWith(commonIndent, StringComparison.Ordinal), "indent expected");

          commentBodyOffset += commonIndent.Length;
        }

        if (isShifted) commentBodyOffset++;

        yield return commentBodyOffset;
      }

      Assertion.Assert(lineIndex == lines.Length, "lineIndex == lines.Length");
    }

    [CanBeNull, Pure]
    private static string GetCommonLinesIndent([NotNull] string[] multilineDocCommentLines)
    {
      string commonIndent = null;

      for (var index = 1; index < multilineDocCommentLines.Length; index++)
      {
        var docCommentLine = multilineDocCommentLines[index];

        if (index == multilineDocCommentLines.Length - 1
            && string.IsNullOrWhiteSpace(docCommentLine)) break;

        var lineIndent = GetStartIndent(docCommentLine);
        if (lineIndent.Length == 0)
        {
          return null;
        }

        if (commonIndent == null)
        {
          commonIndent = lineIndent;
          continue;
        }

        if (lineIndent.StartsWith(commonIndent, StringComparison.Ordinal))
        {
          continue;
        }

        if (commonIndent.StartsWith(lineIndent, StringComparison.Ordinal))
        {
          commonIndent = lineIndent;
          continue;
        }

        return null;
      }

      return commonIndent;

      string GetStartIndent(string line)
      {
        for (var index = 0; index < line.Length; index++)
        {
          var ch = line[index];
          if (char.IsWhiteSpace(ch)) continue;

          if (ch == '*') index++;

          return line.Substring(startIndex: 0, length: index);
        }

        return "";
      }
    }

    protected override IReadOnlyList<ITreeNode> GetDocCommentNodes() => DocCommentBlock.DocComments;

    protected override string GetDocCommentStartText(ITreeNode commentNode) => "///";

    public override void SubTreeChanged()
    {
    }

    protected override IEnumerable<int> GetCommentLinesStartOffsets(ITreeNode commentNode)
    {
      var comment = (IFSharpCommentNode) commentNode;
      if (comment.CommentType == CommentType.MultilineComment)
      {
        return GetMultilineCommentLinesStartOffsets(comment, IsShifted);
      }

      return base.GetCommentLinesStartOffsets(commentNode);
    }
  }
}
