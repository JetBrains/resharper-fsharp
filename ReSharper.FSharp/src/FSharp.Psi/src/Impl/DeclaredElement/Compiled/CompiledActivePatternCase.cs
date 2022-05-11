using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public class CompiledActivePatternCase : DelegatingDeclaredElement, IFSharpGeneratedFromOtherElement,
    IActivePatternCase
  {
    public CompiledActivePatternCase([NotNull] IMethod activePattern, string name, int index) : base(activePattern)
    {
      ShortName = name;
      Index = index;
    }

    public override string ShortName { get; }
    public int Index { get; }

    public override ITypeMember GetContainingTypeMember() => (ITypeMember)Origin;
    public override DeclaredElementType GetElementType() => FSharpDeclaredElementType.ActivePatternCase;

    public override bool Equals(object obj)
    {
      if (!(obj is CompiledActivePatternCase patternCase))
        return false;

      return Index == patternCase.Index && Equals(Origin, patternCase.Origin);
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public string SourceName => ShortName;
    public IClrDeclaredElement OriginElement => Origin;
    public bool IsReadOnly => false;

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new CompiledActivePatternCasePointer(this, ShortName, Index);
  }

  public class CompiledActivePatternCasePointer : FSharpGeneratedElementPointerBase<CompiledActivePatternCase, IMethod>
  {
    public string ShortName { get; }
    public int Index { get; }

    public CompiledActivePatternCasePointer(CompiledActivePatternCase patternCase, string shortName, int index)
      : base(patternCase)
    {
      ShortName = shortName;
      Index = index;
    }

    public override CompiledActivePatternCase CreateGenerated(IMethod fsElement) =>
      new CompiledActivePatternCase(fsElement, ShortName, Index);
  }
}
