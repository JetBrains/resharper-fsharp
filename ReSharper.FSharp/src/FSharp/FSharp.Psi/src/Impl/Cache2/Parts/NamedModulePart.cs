using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class NamedModulePart : DeclaredModulePartBase<INamedModuleDeclaration>
  {
    public NamedModulePart([NotNull] INamedModuleDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.CompiledName),
        ModifiersUtil.GetDecoration(declaration.AccessModifier, declaration.Attributes), cacheBuilder)
    {
    }

    public NamedModulePart(IReader reader) : base(reader)
    {
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.NamedModule;
  }
}
