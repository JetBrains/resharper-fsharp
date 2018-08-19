using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpDeclaredElement : IClrDeclaredElement
  {
  }

  public interface IFSharpTypeMember : IFSharpDeclaredElement, ITypeMember
  {
    bool IsVisibleFromFSharp { get; }
    bool IsExtensionMember { get; }
    bool IsMember { get; }
  }
}