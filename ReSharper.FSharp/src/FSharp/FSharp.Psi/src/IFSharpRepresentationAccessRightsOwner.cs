using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpRepresentationAccessRightsOwner
{
  AccessRights RepresentationAccessRights { get; }
}
