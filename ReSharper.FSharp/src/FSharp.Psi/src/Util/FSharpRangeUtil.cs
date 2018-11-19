using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler;
using Column = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocColumn>;
using Line = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocLine>;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpRangeUtil
  {
    public static int GetDocumentOffset([NotNull] this IDocument document, Line line, Column column)
    {
      return document.GetLineLength(line) >= column
        ? document.GetOffsetByCoords(new DocumentCoords(line, column))
        : document.GetLineEndOffsetNoLineBreak(line);
    }

    public static int GetDocumentOffset([NotNull] this IDocument document, int line, int column)
    {
      return document.GetDocumentOffset((Line) line, (Column) column);
    }

    public static TreeOffset GetTreeOffset([NotNull] IDocument document, Line line, Column column)
    {
      return document.GetLineLength(line) >= column
        ? new TreeOffset(document.GetOffsetByCoords(new DocumentCoords(line, column)))
        : TreeOffset.InvalidOffset;
    }

    public static TreeOffset GetTreeStartOffset([NotNull] this IDocument document, Range.range range)
    {
      return GetTreeOffset(document, range.GetStartLine(), range.GetStartColumn());
    }

    public static TreeOffset GetTreeEndOffset([NotNull] this IDocument document, Range.range range)
    {
      return GetTreeOffset(document, range.GetEndLine(), range.GetEndColumn());
    }

    public static Line GetStartLine(this Range.range range)
    {
      // FCS lines are 1-based
      return (Line) (range.StartLine - 1);
    }

    public static Line GetEndLine(this Range.range range)
    {
      // FCS lines are 1-based
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

    public static Range.pos GetPos([NotNull] this IDocument document, int offset, int lineShift = 0,
      int columnShift = 0)
    {
      // FCS lines are 1-based
      var coords = document.GetCoordsByOffset(offset);
      return Range.mkPos((int) coords.Line + 1, (int) coords.Column);
    }

    public static Range.pos GetPos(this DocumentCoords coords, int columnShift = 0)
    {
      // FCS lines are 1-based
      return Range.mkPos((int) coords.Line + 1, (int) coords.Column + columnShift);
    }

    public static int GetOffset([NotNull] this IDocument document, Range.pos pos)
    {
      return GetDocumentOffset(document, (Line) (pos.Line - 1), (Column) pos.Column);
    }

    public static bool Contains(this Range.range range, DocumentCoords coords) =>
      range.StartLine - 1 >= (int) coords.Line && range.EndLine - 1 <= (int) coords.Line &&
      range.StartColumn >= (int) coords.Column && range.EndColumn <= (int) coords.Column;
  }
}