using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class AnonModulePart : ModulePartBase<IAnonModuleDeclaration>
  {
    public AnonModulePart([NotNull] IAnonModuleDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.CompiledName),
        MemberDecoration.FromModifiers(ReSharper.Psi.Modifiers.PUBLIC), cacheBuilder)
    {
    }

    public AnonModulePart(IReader reader) : base(reader)
    {
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.AnonModule;

    public override bool IsAnonymous => true;
    public override ModuleMembersAccessKind AccessKind => ModuleMembersAccessKind.Normal;
  }
}
