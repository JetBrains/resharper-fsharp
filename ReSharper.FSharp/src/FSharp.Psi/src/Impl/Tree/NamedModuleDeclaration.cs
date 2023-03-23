using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NamedModuleDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetModuleCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => Identifier;

    public bool IsRecursive => RecKeyword != null;

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      ModuleOrNamespaceKeyword.NotNull().AddTokenAfter(FSharpTokenType.REC);
    }

  }
}
