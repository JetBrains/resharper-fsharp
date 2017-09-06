using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class FSharpGlobalNamespaceDeclaration
  {
    public override TreeTextRange GetNameRange() => TreeTextRange.InvalidRange;
    public override IDeclaredElement DeclaredElement => null;
    public override string DeclaredName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public bool IsModule => false;
  }
}