using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpModule : FSharpClass, IModule
  {
    public FSharpModule([NotNull] IModulePart part) : base(part)
    {
    }

    protected override IList<IDeclaredType> CalcSuperTypes() =>
      new[] {Module.GetPredefinedType().Object};

    public bool IsAnonymous =>
      this.GetPart<IModulePart>() is var part && part != null && part.IsAnonymous;

    public bool IsAutoOpen =>
      this.GetPart<IModulePart>() is var part && part != null && part.IsAutoOpen;

    protected override bool AcceptsPart(TypePart part) =>
      part is IModulePart;

    public ITypeElement AssociatedTypeElement =>
      EnumerateParts()
        .Select(part => (part as IModulePart)?.AssociatedTypeElement)
        .WhereNotNull()
        .FirstOrDefault();
  }
}
