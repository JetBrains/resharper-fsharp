using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class NestedModulePart : ModulePartBase<INestedModuleDeclaration>
  {
    public NestedModulePart([NotNull] INestedModuleDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.DeclaredName),
        ModifiersUtil.GetDecoration(declaration.AccessModifiers, declaration.AttributesEnumerable), cacheBuilder)
    {
    }

    public NestedModulePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpModule(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.NestedModule;
  }
}
