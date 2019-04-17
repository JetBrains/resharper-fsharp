using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public abstract class DelegatingDeclaredElement : DeclaredElementBase
  {
    [NotNull] public IClrDeclaredElement Origin { get; }

    protected DelegatingDeclaredElement(IClrDeclaredElement origin) =>
      Origin = origin;

    public override bool IsValid() => Origin.IsValid();
    public override ITypeElement GetContainingType() => Origin.GetContainingType();

    public override IPsiModule Module => Origin.Module;
    public override IPsiServices GetPsiServices() => Origin.GetPsiServices();
    public override PsiLanguageType PresentationLanguage => Origin.PresentationLanguage;

    public override ISubstitution IdSubstitution =>
      GetContainingType()?.IdSubstitution ??
      EmptySubstitution.INSTANCE;
  }
}
