using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override string SourceName => Identifier.GetSourceName();
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();

    public IList<ITypeOwner> GetFields()
    {
      var fieldDeclarations = Fields;
      var result = new LocalList<ITypeOwner>(fieldDeclarations.Count);
      foreach (var fieldDeclaration in fieldDeclarations)
      {
        var field = fieldDeclaration.DeclaredElement;
        if (field != null)
          result.Add((ITypeOwner) field);
      }

      return result.ResultingList();
    }
  }
}
