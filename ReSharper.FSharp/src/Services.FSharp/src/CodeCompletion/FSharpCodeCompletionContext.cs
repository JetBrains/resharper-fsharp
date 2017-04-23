using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Feature.Services.FSharp.CodeCompletion
{
  public class FSharpCodeCompletionContext : SpecificCodeCompletionContext
  {
    public FSharpCodeCompletionContext([NotNull] CodeCompletionContext context, TextLookupRanges ranges,
      TreeOffset caretOffset, DocumentCoords coords, Tuple<FSharpList<string>, string> names,
      ITreeNode tokenBeforeCaret, ITreeNode tokenAtCaret, string lineText) : base(context)
    {
      Ranges = ranges;
      CaretOffset = caretOffset;
      Coords = coords;
      Names = names;
      TokenBeforeCaret = tokenBeforeCaret;
      TokenAtCaret = tokenAtCaret;
      LineText = lineText;
    }

    public override string ContextId => "FSharpCodeCompletionContext";
    public TextLookupRanges Ranges { get; }
    public Tuple<FSharpList<string>, string> Names { get; }
    public ITreeNode TokenBeforeCaret { get; }
    public ITreeNode TokenAtCaret { get; }
    public string LineText { get; }
    public TreeOffset CaretOffset { get; }
    public DocumentCoords Coords { get; }
  }
}