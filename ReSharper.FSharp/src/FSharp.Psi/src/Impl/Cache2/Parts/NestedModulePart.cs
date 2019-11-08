using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class NestedModulePart : DeclaredModulePartBase<INestedModuleDeclaration>
  {
    public NestedModulePart([NotNull] INestedModuleDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.CompiledName),
        ModifiersUtil.GetDecoration(declaration.AccessModifier, declaration.Attributes), cacheBuilder)
    {
    }

    public NestedModulePart(IReader reader) : base(reader)
    {
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.NestedModule;
  }
}
