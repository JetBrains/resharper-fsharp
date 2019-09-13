using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class TypeReference : FSharpSymbolReference
  {
    public TypeReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }

    public override FSharpSymbol GetFSharpSymbol()
    {
      var symbol = base.GetFSharpSymbol();
      if (symbol is FSharpEntity entity)
        return entity;

      if (base.GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv && mfv.IsConstructor)
        return mfv.DeclaringEntity?.Value;

      return null;
    }
  }
  
  public class BaseTypeReference : TypeReference
  {
    public BaseTypeReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }
  }
}
