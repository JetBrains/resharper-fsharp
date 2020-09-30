using FSharp.Compiler;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
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

    public FSharpDisplayContext DisplayContext { get; set; }
    public FSharpXmlDocService XmlDocService { get; set; }

    public IFSharpFile FSharpFile => (IFSharpFile) BasicContext.File;

    public override string ContextId => "FSharpCodeCompletionContext";
    public TextLookupRanges Ranges { get; }
    public PartialLongName PartialLongName { get; }
    [CanBeNull] public ITreeNode TokenBeforeCaret { get; }
    [CanBeNull] public ITreeNode TokenAtCaret { get; }
    public string LineText { get; }
    public FSharpOption<CompletionContext> FsCompletionContext { get; }
    public DocumentCoords Coords { get; }
  }
}