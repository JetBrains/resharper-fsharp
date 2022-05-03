using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpModule : FSharpClass, IFSharpModule, IAlternativeNameCacheTrieNodeOwner
  {
    public CacheTrieNode AlternativeNameTrieNode { get; set; }

    public FSharpModule([NotNull] IModulePart part) : base(part)
    {
    }

    protected override LocalList<IDeclaredType> CalcSuperTypes() =>
      new(new[] { Module.GetPredefinedType().Object });

    private IModulePart ModulePart =>
      this.GetPart<IModulePart>().NotNull();

    public bool IsAnonymous => ModulePart.IsAnonymous;

    public ModuleMembersAccessKind AccessKind => ModulePart.AccessKind;

    public bool IsAutoOpen => AccessKind == ModuleMembersAccessKind.AutoOpen;
    public bool RequiresQualifiedAccess => AccessKind == ModuleMembersAccessKind.RequiresQualifiedAccess;

    protected override bool AcceptsPart(TypePart part) =>
      part is IModulePart && part.ShortName == ShortName;

    public ITypeElement AssociatedTypeElement =>
      EnumerateParts()
        .Select(part => (part as IModulePart)?.AssociatedTypeElement)
        .WhereNotNull()
        .FirstOrDefault();

    string IAlternativeNameOwner.AlternativeName => SourceName != ShortName ? SourceName : null;
  }
}
