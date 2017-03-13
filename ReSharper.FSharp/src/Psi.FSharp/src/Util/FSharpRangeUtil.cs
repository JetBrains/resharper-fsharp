using JetBrains.DocumentModel;
using Microsoft.FSharp.Compiler;
using Column = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocColumn>;
using Line = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocLine>;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public class FSharpRangeUtil
  {
    public static int GetDocumentOffset(IDocument document, Line line, Column column)
    {
      return document.GetLineLength(line) >= column
        ? document.GetOffsetByCoords(new DocumentCoords(line, column))
        : document.GetLineEndOffsetNoLineBreak(line);
    }

    public static TreeOffset GetTreeOffset(IDocument document, Line line, Column column)
    {
      return document.GetLineLength(line) < column
        ? TreeOffset.InvalidOffset
        : new TreeOffset(document.GetOffsetByCoords(new DocumentCoords(line, column)));
    }

    public static TreeOffset GetStartOffset(IDocument document, Range.range range)
    {
      return GetTreeOffset(document, (Line) (range.StartLine - 1), (Column) range.StartColumn);
    }

    public static TreeOffset GetEndOffset(IDocument document, Range.range range)
    {
      return GetTreeOffset(document, (Line) (range.EndLine - 1), (Column) range.EndColumn);
    }
  }
}