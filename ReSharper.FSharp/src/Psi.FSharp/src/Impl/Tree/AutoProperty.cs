using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class AutoProperty
  {
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public override FSharpSymbol GetFSharpSymbol()
    {
      var nameRange = GetNameRange();
      if (!nameRange.IsValid())
        return null;

      var fsFile = this.GetContainingFile() as IFSharpFile;
      Assertion.AssertNotNull(fsFile, "fsFile != null");
      var token = fsFile.FindTokenAt(nameRange.StartOffset);
      if (token == null)
        return null;

      return FSharpSymbolsUtil.TryFindFSharpSymbol(fsFile, token.GetText(), nameRange.EndOffset.Offset);
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var mfv = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      if (mfv == null) return null;

      var entityMembers = mfv.EnclosingEntity.MembersFunctionsAndValues; //todo inheritance with same name
      var property = entityMembers?.SingleItem(m => m.IsProperty && !m.IsPropertyGetterMethod &&
                                                    !m.IsPropertySetterMethod && m.DisplayName == mfv.DisplayName);
      if (property == null)
        return null;
      return new FSharpProperty<AutoProperty>(this, property);
    }
  }
}