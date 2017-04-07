using JetBrains.DocumentModel;
using Microsoft.FSharp.Compiler;
using Column = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocColumn>;
using Line = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocLine>;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public static class FSharpRangeUtil
  {
    public static int GetDocumentOffset(IDocument document, Line line, Column column)
    {
      return document.GetLineLength(line) >= column
        ? document.GetOffsetByCoords(new DocumentCoords(line, column))
        : document.GetLineEndOffsetNoLineBreak(line);
    }

    public static TreeOffset GetTreeOffset(IDocument document, Line line, Column column)
    {
      return document.GetLineLength(line) >= column
        ? new TreeOffset(document.GetOffsetByCoords(new DocumentCoords(line, column)))
        : TreeOffset.InvalidOffset;
    }

    public static TreeOffset GetTreeStartOffset(this IDocument document, Range.range range)
    {
      return GetTreeOffset(document, range.GetStartLine(), range.GetStartColumn());
    }

    public static TreeOffset GetTreeEndOffset(this IDocument document, Range.range range)
    {
      return GetTreeOffset(document, range.GetEndLine(), range.GetEndColumn());
    }

    public static Line GetStartLine(this Range.range range)
    {
      return (Line) (range.StartLine - 1);
    }

    public static Line GetEndLine(this Range.range range)
    {
      return (Line) (range.EndLine - 1);
    }

    public static Column GetStartColumn(this Range.range range)
    {
      return (Column) range.StartColumn;
    }

    public static Column GetEndColumn(this Range.range range)
    {
      return (Column) range.EndColumn;
    }
  }
}