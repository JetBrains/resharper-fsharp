using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public class CompiledActivePatternCase([NotNull] IMethod activePattern, string name, int index)
    : DelegatingDeclaredElement(activePattern), IFSharpGeneratedFromOtherElement, IActivePatternCase
  {
    public override string ShortName { get; } = name;
    public int Index { get; } = index;

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

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new CompiledActivePatternCasePointer(this, ShortName, Index);
    
    public DeclaredElementType FSharpElementType => FSharpDeclaredElementType.ActivePatternCase;
  }

  public class CompiledActivePatternCasePointer(CompiledActivePatternCase patternCase, string shortName, int index)
    : FSharpGeneratedElementPointerBase<CompiledActivePatternCase, IMethod>(patternCase)
  {
    public string ShortName { get; } = shortName;
    public int Index { get; } = index;

    public override CompiledActivePatternCase CreateGenerated(IMethod fsElement) => new(fsElement, ShortName, Index);
  }
}
