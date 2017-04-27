using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpUnion : FSharpClassLikeElement<UnionPart>
  {
    public FSharpUnion([NotNull] IClassPart part) : base(part)
    {
    }

    public IEnumerable<FSharpUnionCase> Cases => NestedTypes.OfType<FSharpUnionCase>();

    public override IEnumerable<ITypeMember> GetMembers()
    {
      var intType = Module.GetPredefinedType().Int;
      var boolType = Module.GetPredefinedType().Bool;
      var thisType = TypeFactory.CreateType(this);

      var generatedMembers = new LocalList<ITypeMember>();
      foreach (var unionCase in Cases)
      {
        generatedMembers.Add(new FSharpGeneratedProperty(this, "Is" + unionCase.ShortName, boolType));

        if (unionCase.IsSingletonCase)
          generatedMembers.Add(new FSharpGeneratedProperty(this, unionCase.ShortName, thisType, true));
        else
        {
          var fields = unionCase.CaseFields.AsArray();
          var types = fields.Convert(f => f.Type);
          var names = fields.Convert(f => f.ShortName);
          generatedMembers.Add(new FSharpGeneratedMethod(this, "New" + unionCase.ShortName, types, names, thisType,
            false, true));
        }
      }

      generatedMembers.Add(new FSharpGeneratedProperty(this, "Tag", intType));
      generatedMembers.Add(new FSharpTagsClass(this));

      return base.GetMembers().Prepend(generatedMembers.ResultingList());
    }
  }
}