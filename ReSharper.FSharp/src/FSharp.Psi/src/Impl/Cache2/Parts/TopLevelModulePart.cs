using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class TopLevelModulePart : ModulePartBase<ITopLevelModuleDeclaration>
  {
    public TopLevelModulePart([NotNull] ITopLevelModuleDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.CompiledName),
        ModifiersUtil.GetDecoration(declaration.AccessModifiers, declaration.AttributesEnumerable), cacheBuilder)
    {
    }

    public TopLevelModulePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpModule(this);
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.TopLevelModule;
  }
}