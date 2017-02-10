using JetBrains.DocumentModel;
using Column = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocColumn>;
using Line = JetBrains.Util.dataStructures.TypedIntrinsics.Int32<JetBrains.DocumentModel.DocLine>;

namespace JetBrains.ReSharper.Psi.FSharp
{
  public class FSharpRangeUtil
  {
    public static int GetDocumentOffset(IDocument document, Line line, Column column)
    {
      var lineLength = document.GetLineLength(line);
      if (column > lineLength) column = lineLength;
      return document.GetOffsetByCoords(new DocumentCoords(line, column));
    }
  }
}