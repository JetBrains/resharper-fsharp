using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
{
  public class FSharpCodeCompletionContext : SpecificCodeCompletionContext
  {
    public FSharpCodeCompletionContext([NotNull] CodeCompletionContext context,
      FSharpOption<CompletionContext> fsCompletionContext, TextLookupRanges ranges, DocumentCoords coords,
      PartialLongName partialLongName, ITreeNode tokenBeforeCaret = null, ITreeNode tokenAtCaret = null,
      string lineText = null) : base(context)
    {
      Ranges = ranges;
      Coords = coords;
      PartialLongName = partialLongName;
      TokenBeforeCaret = tokenBeforeCaret;
      TokenAtCaret = tokenAtCaret;
      LineText = lineText;
      FsCompletionContext = fsCompletionContext;
    }

    public override string ContextId => "FSharpCodeCompletionContext";
    public TextLookupRanges Ranges { get; }
    public PartialLongName PartialLongName { get; }
    public ITreeNode TokenBeforeCaret { get; }
    public ITreeNode TokenAtCaret { get; }
    public string LineText { get; }
    public FSharpOption<CompletionContext> FsCompletionContext { get; }
    public DocumentCoords Coords { get; }
  }
}