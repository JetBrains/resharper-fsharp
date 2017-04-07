using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Debugger;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using Range = Microsoft.FSharp.Compiler.Range;

namespace JetBrains.ReSharper.Feature.Services.FSharp.Debugger
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpBreakpointVariantsProvider : IBreakpointVariantsProvider
  {
    private const string MultilineBreakpointTextSuffix = " ...";

    public List<BreakpointVariantModelBase> GetBreakpointVariants(IProjectFile file, int line, ISolution solution)
    {
      var fsFile = file.GetPrimaryPsiFile() as IFSharpFile;
      var parseResults = fsFile?.ParseResults;
      var document = file.ToSourceFile()?.Document;
      if (parseResults == null || document == null)
        return null;

      var breakpointVariants = new JetHashSet<BreakpointVariantModelBase>();
      var documentLine = (Int32<DocLine>) line;
      var lineEndOffset = document.GetLineEndOffsetWithLineBreak(documentLine);
      var token = fsFile.FindTokenAt(new TreeOffset(document.GetLineStartOffset(documentLine)));
      while (token != null && token.GetTreeEndOffset().Offset < lineEndOffset)
      {
        var rangeOption = parseResults.ValidateBreakpointLocation(GetPos(document, token.GetTreeEndOffset().Offset));
        if (rangeOption == null || rangeOption.Value.StartLine - 1 != line)
        {
          token = token.GetNextToken();
          continue;
        }

        var range = rangeOption.Value;
        var startOffset = FSharpRangeUtil.GetStartOffset(document, range).Offset;
        var endOffset = FSharpRangeUtil.GetEndOffset(document, range).Offset;
        var breakpointLineText = document.GetText(new TextRange(startOffset, Math.Min(lineEndOffset, endOffset)));
        var breakpointText = endOffset > lineEndOffset
          ? breakpointLineText + MultilineBreakpointTextSuffix
          : breakpointLineText;

        breakpointVariants.Add(new BreakpointVariantModel(startOffset, endOffset, breakpointText));
        token = token.GetNextToken();
      }
      return breakpointVariants.ToList();
    }

    private static Range.pos GetPos([NotNull] IDocument document, int offset)
    {
      var coords = document.GetCoordsByOffset(offset);
      return Range.mkPos((int) coords.Line + 1, (int) coords.Column);
    }
  }
}