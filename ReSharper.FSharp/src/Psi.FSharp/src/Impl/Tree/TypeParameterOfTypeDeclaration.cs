using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class TypeParameterOfTypeDeclaration : ICachedDeclaration2
  {
    public override string DeclaredName => Identifier.GetName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public override void SetName(string name)
    {
    }

    protected override void PreInit()
    {
      base.PreInit();
      CacheDeclaredElement = null;
    }

    public override IDeclaredElement DeclaredElement
    {
      get
      {
        Assertion.Assert(IsValid(), "Getting declared element from invalid declaration");
        Assertion.Assert(CacheDeclaredElement == null || CacheDeclaredElement.IsValid(),
          "myCacheDeclaredElement == null || myCacheDeclaredElement.IsValid()");
        return CacheDeclaredElement;
      }
    }

    public IDeclaredElement CacheDeclaredElement { get; set; }
  }
}