using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpUnion : FSharpSimpleTypeBase
  {
    public FSharpUnion([NotNull] IClassPart part) : base(part)
    {
    }

    protected override bool EmitsFieldsConstructor => false;

    public IEnumerable<ITypeMember> Cases =>
      base.GetMembers().Where(member => member is FSharpUnionCase || member is FSharpUnionCaseProperty);

    public override IEnumerable<ITypeMember> GetMembers()
    {
      var predefinedType = Module.GetPredefinedType();
      var unionType = TypeFactory.CreateType(this);
      var cases = Cases.AsCollection();
      var isSingleCaseUnion = cases.Count == 1;

      var members = new LocalList<ITypeMember>();
      foreach (var unionCase in cases)
      {
        var caseName = unionCase.ShortName;
        if (!isSingleCaseUnion)
          members.Add(new FSharpGeneratedProperty(this, "Is" + caseName, predefinedType.Bool));

        var typedCase = unionCase as FSharpUnionCase;
        if (typedCase == null) continue;

        var fields = typedCase.CaseFields.AsArray();
        var types = fields.Convert(f => f.Type);
        var names = fields.Convert(f => f.ShortName);
        members.Add(new FSharpGeneratedMethod(this, "New" + caseName, types, names, unionType, isStatic: true));
      }

      var theOnlyCase = isSingleCaseUnion ? cases.FirstOrDefault() as FSharpUnionCase : null;
      if (theOnlyCase != null)
        members.AddRange(theOnlyCase.CaseFields);

      members.Add(new FSharpGeneratedProperty(this, "Tag", predefinedType.Int));
      if (!isSingleCaseUnion)
        members.Add(new FSharpUnionTagsClass(this));

      return base.GetMembers().Prepend(members.ResultingList());
    }
  }
}