using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpGeneratedConstructorOwnerPart
{
  [CanBeNull]
  IFSharpParameterOwner GetConstructor();
}
