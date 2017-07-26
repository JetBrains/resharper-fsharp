using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts
{
  internal class TopLevelModulePart : ModulePartBase<ITopLevelModuleDeclaration>
  {
    public TopLevelModulePart([NotNull] ITopLevelModuleDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.ShortName),
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