using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

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
    public IFSharpTypeDeclaration GetAssociatedTypeDeclaration(out string sourceName)
    {
      sourceName = null;

      if (!(Parent is IModuleLikeDeclaration parentModule))
        return null;

      sourceName = SourceName;
      foreach (var typeDeclaration in parentModule.Children<IFSharpTypeDeclaration>())
        if (typeDeclaration.CompiledName == sourceName && typeDeclaration.TypeParameters.IsEmpty)
          return typeDeclaration;

      return null;
    }

    public override IFSharpIdentifierLikeNode NameIdentifier => (IFSharpIdentifierLikeNode) Identifier;
  }
}
