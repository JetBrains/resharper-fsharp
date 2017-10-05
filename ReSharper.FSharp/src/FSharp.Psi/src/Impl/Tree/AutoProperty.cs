using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AutoProperty
  {
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();

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
      if (!(GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv)) return null;

      var entityMembers = mfv.EnclosingEntity?.Value.MembersFunctionsAndValues; //todo inheritance with same name
      var property = entityMembers?.SingleItem(m => m.IsProperty && !m.IsPropertyGetterMethod &&
                                                    !m.IsPropertySetterMethod && m.DisplayName == mfv.DisplayName);
      return property == null
        ? null
        : new FSharpProperty<AutoProperty>(this, property);
    }
  }
}