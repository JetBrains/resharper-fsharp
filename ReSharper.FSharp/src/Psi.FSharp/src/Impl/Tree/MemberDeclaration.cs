using System;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class MemberDeclaration : IFunctionDeclaration
  {
    IFunction IFunctionDeclaration.DeclaredElement => base.DeclaredElement as IFunction;
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);
    public override string SourceName => FSharpImplUtil.GetSourceName(Identifier);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var mfv = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      if (mfv == null) return null;

      if (mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod)
      {
        var property = mfv.TryGetPropertyFromAccessor();
        return property != null
          ? new FSharpProperty<MemberDeclaration>(this, property)
          : null;
      }

      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeDeclaration;
      return new FSharpMethod<MemberDeclaration>(this, mfv, typeDeclaration);
//      return !mfv.IsInstanceMember && mfv.CompiledName.StartsWith("op_", StringComparison.Ordinal)
//        ? (IDeclaredElement) new FSharpOperator<MemberDeclaration>(this, mfv, null)
//        : new FSharpMethod<MemberDeclaration>(this, mfv, typeDeclaration);
    }
  }
}