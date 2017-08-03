using System;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class Let : IFunctionDeclaration
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

      if (!mfv.IsValCompiledAsMethod)
        return new ModuleValue(this, mfv);

      return !mfv.IsInstanceMember && mfv.CompiledName.StartsWith("op_", StringComparison.Ordinal)
        ? (IDeclaredElement) new FSharpOperator<Let>(this, mfv, null)
        : new ModuleFunction(this, mfv, null);
    }
  }
}