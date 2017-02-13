using JetBrains.Annotations;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public class FSharpSymbolUtil
  {
    private const string FSharpConstructorName = "( .ctor )";
    private const string ClrConstructorName = ".ctor";

    [CanBeNull]
    public static string GetDisplayName(FSharpSymbol symbol)
    {
      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        var name = mfv.DisplayName;
        if (name == FSharpConstructorName || name == ClrConstructorName)
          return mfv.EnclosingEntity.DisplayName;
        if (mfv.IsMember) return name;
      }

      var fsField = symbol as FSharpField;
      if (fsField != null) return fsField.DisplayName;

      return symbol.DeclarationLocation == null ? null : symbol.DisplayName;
    }

    public static bool IsEscapedName([NotNull] string name)
    {
      var length = name.Length;
      return length > 4 && name[0] == '(' && name[1] == ' ' && name[length - 2] == ' ' && name[length - 1] == ')';
    }
  }
}