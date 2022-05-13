using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.TypeProviders
{
  [SolutionComponent]
  public class ProvidedAbbreviationTypePartInvalidator
  {
    private readonly IProxyExtensionTypingProvider myTypeProvidersShim;

    public ProvidedAbbreviationTypePartInvalidator(Lifetime lifetime, IProxyExtensionTypingProvider typeProvidersShim,
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
      if (myTypeProvidersShim.TypeProvidersManager is not { } tpManager) return;
      if (typePart.TypeElement is not { } typeElement) return;
      tpManager.Context.ProvidedAbbreviations.MarkAsInvalidated(typeElement.Module, typeElement.GetClrName());
    }
  }
}
