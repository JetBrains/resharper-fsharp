using JetBrains.ReSharper.Plugins.FSharp.Common.Naming;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NestedModuleDeclaration
  {
    protected override FSharpName GetFSharpName()
    {
      if (!(Parent is IModuleLikeDeclaration parentModule))
        return Identifier.GetModuleCompiledName(Attributes);

      var sourceName = Identifier.GetName();
      foreach (var typeDeclaration in parentModule.Children<IFSharpTypeDeclaration>())
        if (typeDeclaration.ShortName == sourceName && typeDeclaration.TypeParameters.IsEmpty)
          return FSharpName.NewMultipleNames(sourceName, sourceName + "Module", CompiledNameKind.ImplicitModule);

      return Identifier.GetModuleCompiledName(Attributes);
    }

    public bool IsModule => true;
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();

    public override void SetName(string name) =>
      Identifier.ReplaceIdentifier(name);
  }
}
