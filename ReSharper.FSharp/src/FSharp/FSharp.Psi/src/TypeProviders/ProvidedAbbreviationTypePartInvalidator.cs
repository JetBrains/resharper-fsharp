using JetBrains.Application.Parts;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.TypeProviders
{
  [SolutionComponent(InstantiationEx.LegacyDefault)]
  public class ProvidedAbbreviationTypePartInvalidator
  {
    private readonly ITypeProvidersShim myTypeProvidersShim;

    public ProvidedAbbreviationTypePartInvalidator(Lifetime lifetime, ITypeProvidersShim typeProvidersShim,
      ISymbolCache symbolCache)
    {
      myTypeProvidersShim = typeProvidersShim;
      lifetime.Bracket(
        () => symbolCache.OnBeforeTypePartRemoved += InvalidateTypePart,
        () => symbolCache.OnBeforeTypePartRemoved -= InvalidateTypePart);
    }

    private void InvalidateTypePart(TypePart typePart)
    {
      if (typePart is not TypeAbbreviationOrDeclarationPart) return;
      if (myTypeProvidersShim.SolutionTypeProvidersClient is not { } tpManager) return;
      if (typePart.TypeElement is not { } typeElement) return;
      tpManager.Context.ProvidedAbbreviations.MarkDirty(typeElement.Module, typeElement.GetClrName());
    }
  }
}
