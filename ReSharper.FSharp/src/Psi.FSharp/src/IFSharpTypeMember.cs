namespace JetBrains.ReSharper.Psi.FSharp
{
  public interface IFSharpTypeMember : ITypeMember
  {
    bool IsVisibleFromFSharp { get; }
  }
}