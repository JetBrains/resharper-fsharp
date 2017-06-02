using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpUnion : FSharpTypeBase
  {
    public FSharpUnion([NotNull] IClassPart part) : base(part)
    {
    }

    public IEnumerable<ITypeMember> Cases
    {
      get
      {
        foreach (var member in base.GetMembers())
          if (member is FSharpUnionCase || member is FSharpFieldProperty)
            yield return member;
      }
    }

    public override IEnumerable<ITypeMember> GetMembers()
    {
      var intType = Module.GetPredefinedType().Int;
      var boolType = Module.GetPredefinedType().Bool;
      var thisType = TypeFactory.CreateType(this);

      var generatedMembers = new LocalList<ITypeMember>();
      foreach (var unionCase in Cases)
      {
        generatedMembers.Add(new FSharpGeneratedProperty(this, "Is" + unionCase.ShortName, boolType));

        var typedUnionCase = unionCase as FSharpUnionCase;
        if (typedUnionCase == null)
          continue;

        var typedCase = typedUnionCase;
        var fields = typedCase.CaseFields.AsArray();
        var types = fields.Convert(f => f.Type);
        var names = fields.Convert(f => f.ShortName);
        generatedMembers.Add(new FSharpGeneratedMethod(this, "New" + unionCase.ShortName, types, names, thisType,
          false, true));
      }

      generatedMembers.Add(new FSharpGeneratedProperty(this, "Tag", intType));
      generatedMembers.Add(new FSharpTagsClass(this));

      return base.GetMembers().Prepend(generatedMembers.ResultingList());
    }
  }
}