using System;
using System.Collections.Generic;
using System.Linq;
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
        var rangeOption = parseResults.ValidateBreakpointLocation(document.GetPos(token.GetTreeEndOffset().Offset));
        if (rangeOption == null || rangeOption.Value.StartLine - 1 != line)
        {
          token = token.GetNextToken();
          continue;
        }

        var range = rangeOption.Value;
        var startOffset = document.GetTreeStartOffset(range).Offset;
        var endOffset = document.GetTreeEndOffset(range).Offset;
        var breakpointLineText = document.GetText(new TextRange(startOffset, Math.Min(lineEndOffset, endOffset)));
        var breakpointVariantText = endOffset > lineEndOffset
          ? breakpointLineText + MultilineBreakpointTextSuffix
          : breakpointLineText;

        breakpointVariants.Add(new BreakpointVariantModel(startOffset, endOffset, breakpointVariantText));
        token = token.GetNextToken();
      }
      return breakpointVariants.ToList();
    }
  }
}