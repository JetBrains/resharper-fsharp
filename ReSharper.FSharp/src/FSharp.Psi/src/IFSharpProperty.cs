using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpProperty : IProperty, IRepresentationAccessRightsOwner
  {
    [NotNull] public IEnumerable<IFSharpExplicitAccessor> FSharpExplicitGetters { get; }
    [NotNull] public IEnumerable<IFSharpExplicitAccessor> FSharpExplicitSetters { get; }
  }
}
