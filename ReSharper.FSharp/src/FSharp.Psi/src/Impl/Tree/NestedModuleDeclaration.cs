using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NestedModuleDeclaration
  {
    protected override string DeclaredElementName
    {
      get
      {
        if (GetAssociatedTypeDeclaration(out var sourceName) != null)
          return sourceName + "Module";

        return NameIdentifier.GetModuleCompiledName(Attributes);
      }
    }

    [CanBeNull]
    public IFSharpTypeOldDeclaration GetAssociatedTypeDeclaration(out string sourceName)
    {
      sourceName = null;

      if (!(Parent is IModuleLikeDeclaration parentModule))
        return null;

      sourceName = SourceName;
      foreach (var moduleMember in parentModule.Members)
      {
        // Only type declarations are taken into account, exception declarations are ignored.
        if (!(moduleMember is ITypeDeclarationGroup typeDeclarationGroup)) 
          continue;

        foreach (var typeDeclaration in typeDeclarationGroup.TypeDeclarations)
          if (typeDeclaration.CompiledName == sourceName && typeDeclaration.TypeParameters.IsEmpty)
            return typeDeclaration;
      }

      return null;
    }

    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    public bool IsRecursive => RecKeyword != null;

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      ModuleOrNamespaceKeyword.NotNull().AddTokenAfter(FSharpTokenType.REC);
    }
  }
}
