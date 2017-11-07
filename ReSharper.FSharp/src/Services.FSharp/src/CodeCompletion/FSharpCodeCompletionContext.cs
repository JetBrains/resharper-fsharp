using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
{
  public class FSharpCodeCompletionContext : SpecificCodeCompletionContext
  {
    public FSharpCodeCompletionContext([NotNull] CodeCompletionContext context, TextLookupRanges ranges,
      TreeOffset caretOffset, DocumentCoords coords, Tuple<FSharpList<string>, string> names,
      ITreeNode tokenBeforeCaret, ITreeNode tokenAtCaret, string lineText, CompletionContext fsCompletionContext, bool shouldComplete = true) : base(context)
    {
      Ranges = ranges;
      CaretOffset = caretOffset;
      Coords = coords;
      Names = names;
      TokenBeforeCaret = tokenBeforeCaret;
      TokenAtCaret = tokenAtCaret;
      LineText = lineText;
      FsCompletionContext = OptionModule.OfObj(fsCompletionContext);
      ShouldComplete = shouldComplete;
    }

    public override string ContextId => "FSharpCodeCompletionContext";
    public TextLookupRanges Ranges { get; }
    public Tuple<FSharpList<string>, string> Names { get; }
    public ITreeNode TokenBeforeCaret { get; }
    public ITreeNode TokenAtCaret { get; }
    public string LineText { get; }
    public FSharpOption<CompletionContext> FsCompletionContext { get; }
    public TreeOffset CaretOffset { get; }
    public DocumentCoords Coords { get; }
    public bool ShouldComplete { get; }
  }
}