namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IBinding : IParameterOwnerMemberDeclaration
  {
    bool IsInline { get; }
    void SetIsInline(bool value);

    bool HasParameters { get; }
  }
}
