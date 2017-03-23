using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class Let

  {
  public override string DeclaredName => Identifier.GetName();

  public override TreeTextRange GetNameRange()
  {
    return Identifier.GetNameRange();
  }

  public override void SetName(string name)
  {
  }

  protected override IDeclaredElement CreateDeclaredElement()
  {
    var mfv = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
    if (mfv != null && mfv.IsValCompiledAsMethod)
      return new ModuleFunction(this, mfv, null);

    return new ModuleValue(this, mfv);
  }
  }
}