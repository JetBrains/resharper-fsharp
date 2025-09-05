using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.Util;

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

    public string ClrName
    {
      get
      {
        var ns = NamespaceName;
        if (!ns.IsEmpty() && ns != SharedImplUtil.MISSING_DECLARATION_NAME)
          return ns + "." + CompiledName;

        return CompiledName;
      }
    }

    public string NamespaceName =>
      QualifierReferenceName?.QualifiedName ??
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public string QualifiedName
    {
      get
      {
        var ns = NamespaceName;
        if (!ns.IsEmpty() && ns != SharedImplUtil.MISSING_DECLARATION_NAME)
          return ns + "." + SourceName;

        return CompiledName;
      }
    }
  }
}
