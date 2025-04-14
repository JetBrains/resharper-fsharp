using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpProperty : IProperty, IFSharpRepresentationAccessRightsOwner
  {
    public bool HasExplicitAccessors { get; }
    public IEnumerable<IFSharpExplicitAccessor> GetExplicitAccessors();

    [NotNull] public IEnumerable<IFSharpExplicitAccessor> FSharpExplicitGetters { get; }
    [NotNull] public IEnumerable<IFSharpExplicitAccessor> FSharpExplicitSetters { get; }
  }
}
