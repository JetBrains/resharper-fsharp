using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class DeclaredModulePartBase<T> : ModulePartBase<T>
    where T : class, IDeclaredModuleDeclaration
  {
    protected DeclaredModulePartBase([NotNull] T declaration, [NotNull] string shortName,
      MemberDecoration memberDecoration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, shortName, memberDecoration, cacheBuilder) =>
      IsAutoOpen = GetDeclaration() is var moduleDeclaration && moduleDeclaration.IsAutoOpen();

    protected DeclaredModulePartBase(IReader reader) : base(reader) =>
      IsAutoOpen = reader.ReadBool();

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(IsAutoOpen);
    }

    public override bool IsAutoOpen { get; }
  }
}
