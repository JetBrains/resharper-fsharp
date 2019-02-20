using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpDeclaredElement : IClrDeclaredElement
  {
    string SourceName { get; }
  }

  public interface IFSharpTypeMember : IFSharpDeclaredElement, ITypeMember
  {
    bool IsVisibleFromFSharp { get; }
    bool CanNavigateTo { get; }

    bool IsExtensionMember { get; }
    bool IsMember { get; }
  }
}