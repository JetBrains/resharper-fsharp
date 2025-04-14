using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpModule([NotNull] IModulePart part)
    : FSharpClass(part), IFSharpModule, IAlternativeNameCacheTrieNodeOwner
  {
    public CacheTrieNode AlternativeNameTrieNode { get; set; }

    protected override LocalList<IDeclaredType> CalcSuperTypes() =>
      new([Module.GetPredefinedType().Object]);

    private IModulePart ModulePart =>
      this.GetPart<IModulePart>().NotNull();

    public bool IsAnonymous => ModulePart.IsAnonymous;

    public override ModuleMembersAccessKind AccessKind => ModulePart.AccessKind;

    public bool IsAutoOpen => AccessKind == ModuleMembersAccessKind.AutoOpen;
    public bool RequiresQualifiedAccess => AccessKind == ModuleMembersAccessKind.RequiresQualifiedAccess;

    protected override bool AcceptsPart(TypePart part) =>
      part is IModulePart && part.ShortName == ShortName;

    public ITypeElement AssociatedTypeElement =>
      EnumerateParts()
        .Select(part => (part as IModulePart)?.AssociatedTypeElement)
        .WhereNotNull()
        .FirstOrDefault();

    /// Qualified source name
    public string QualifiedSourceName => this.GetQualifiedName();

    private string[] GetNames(Func<IModulePart, string[]> getter)
    {
      var definedLabels =
        EnumerateParts()
          .SelectNotNull(part => part is IModulePart modulePart ? getter(modulePart) : null);

      var result = new LocalList<string>();
      foreach (var definedLabel in definedLabels)
        result.AddRange(definedLabel);

      return result.ToArray();
    }

    public string[] ValueNames => GetNames(part => part.ValueNames);
    public string[] FunctionNames => GetNames(part => part.FunctionNames);
    public string[] LiteralNames => GetNames(part => part.LiteralNames);
    public string[] ActivePatternNames => GetNames(part => part.ActivePatternNames);
    public string[] ActivePatternCaseNames => GetNames(part => part.ActivePatternCaseNames);

    string IAlternativeNameOwner.AlternativeName => SourceName != ShortName ? SourceName : null;

    public override string ToString() => this.TestToString(BuildTypeParameterString());
  }

  internal static class FSharpModuleUtil
  {
    public static string GetQualifiedName(this IFSharpModule fsModule)
    {
      var builder = new StringBuilder(fsModule.SourceName);

      var containingType = fsModule.GetContainingType();
      while (containingType != null)
      {
        builder.Prepend(".");
        builder.Prepend(containingType.GetSourceName());
        containingType = containingType.GetContainingType();
      }

      var ns = fsModule.GetContainingNamespace();
      if (!ns.IsRootNamespace)
      {
        builder.Prepend(".");
        builder.Prepend(ns.QualifiedName);  
      }

      return builder.ToString();
    }
  }
}
