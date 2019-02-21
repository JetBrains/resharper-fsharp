using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedElementBase : FSharpDeclaredElementBase
  {
    [NotNull] protected abstract IClrDeclaredElement ContainingElement { get; }

    public override bool IsValid() => ContainingElement.IsValid();
    public override IPsiServices GetPsiServices() => ContainingElement.GetPsiServices();

    public override IPsiModule Module => ContainingElement.Module;
    public override ISubstitution IdSubstitution => ContainingElement.IdSubstitution;

    public virtual bool IsVisibleFromFSharp => false;
    public virtual bool CanNavigateTo => false;
  }
}
