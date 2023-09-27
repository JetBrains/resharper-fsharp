using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class DeclaredModulePartBase<T> : ModulePartBase<T>
    where T : class, IDeclaredModuleDeclaration
  {
    public override ModuleMembersAccessKind AccessKind { get; }

    protected DeclaredModulePartBase([NotNull] T declaration, [NotNull] string shortName,
      MemberDecoration memberDecoration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, shortName, memberDecoration, cacheBuilder) =>
      AccessKind = declaration.GetAccessType();

    protected DeclaredModulePartBase(IReader reader) : base(reader) =>
      AccessKind = (ModuleMembersAccessKind) reader.ReadByte();

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteByte((byte) AccessKind);
    }
  }
}
