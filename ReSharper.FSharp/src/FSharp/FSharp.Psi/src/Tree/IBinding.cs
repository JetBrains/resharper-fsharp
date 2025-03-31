namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IBinding : IParameterOwnerMemberDeclaration
  {
    bool IsInline { get; }
    void SetIsInline(bool value);

    bool HasParameters { get; }

    /// Is compiled to a .NET literal
    bool IsLiteral { get; }
    bool IsComputed { get; }
  }
}
