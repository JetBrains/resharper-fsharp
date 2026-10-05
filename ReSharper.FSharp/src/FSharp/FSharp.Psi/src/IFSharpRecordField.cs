using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpRecordField : IFSharpFunctionalTypeField, IFSharpRepresentationAccessRightsOwner,
  IFSharpMutableModifierOwner, IProperty
{
  bool IsOwnField { get; }
}
