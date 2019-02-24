using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class TopActivePatternCase : FSharpCachedTypeMemberBase<TopActivePatternCaseDeclaration>,
    IFSharpDeclaredElement, IActivePatternCase
  {
    public TopActivePatternCase(IFSharpDeclaration declaration) : base(declaration)
    {
    }

    public override DeclaredElementType GetElementType() => FSharpDeclaredElementType.ActivePatternCase;
    public override string ShortName => GetDeclaration()?.CompiledName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public ITypeMember GetContainingTypeMember() =>
      (ITypeMember) GetContainingType();
  }
}
