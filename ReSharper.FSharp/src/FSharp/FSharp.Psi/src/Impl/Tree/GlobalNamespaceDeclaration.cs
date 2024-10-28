using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class GlobalNamespaceDeclaration
  {
    public override TreeTextRange GetNameRange() => TreeTextRange.InvalidRange;
    public override IDeclaredElement DeclaredElement => null;
    public override string CompiledName => SharedImplUtil.MISSING_DECLARATION_NAME;
    public string ClrName => "";

    public override IFSharpIdentifier NameIdentifier => null;

    public bool IsRecursive => RecKeyword != null;

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      ModuleOrNamespaceKeyword.NotNull().AddTokenAfter(FSharpTokenType.REC);
    }
  }
}
