using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpSimpleTypeBase : FSharpClass
  {
    private const string CompareToName = "CompareTo";
    private const string EqualsName = "Equals";
    private const string GetHashCodeName = "GetHashCode";
    private const string ToStringName = "ToString";
    private const string ObjName = "obj";
    private const string CompName = "comp";
    private const string ComparerTypeName = "System.Collections.IComparer";
    private const string EqComparerTypeName = "System.Collections.IEqualityComparer";

    protected virtual bool OverridesCompareTo => true;
    protected virtual bool OverridesToString => true;
    protected virtual bool EmitsFieldsConstructor => true;

    public FSharpSimpleTypeBase([NotNull] IClassPart part) : base(part)
    {
    }

    public override IEnumerable<ITypeMember> GetMembers()
    {
      // todo: convert these members from FCS and cache them
      var boolType = Module.GetPredefinedType().Bool;
      var intType = Module.GetPredefinedType().Int;
      var objType = Module.GetPredefinedType().Object;
      var stringType = Module.GetPredefinedType().String;
      var thisType = TypeFactory.CreateType(this);
      var compType = TypeFactory.CreateTypeByCLRName(ComparerTypeName, Module);
      var eqCompType = TypeFactory.CreateTypeByCLRName(EqComparerTypeName, Module);

      var members = new LocalList<ITypeMember>();

      members.Add(new FSharpGeneratedMethod(this, EqualsName, thisType, ObjName, boolType));
      members.Add(new FSharpGeneratedMethod(this, EqualsName, objType, ObjName, boolType, isOverride: true));
      members.Add(new FSharpGeneratedMethod(this, EqualsName, objType, ObjName, eqCompType, CompName, boolType));

      members.Add(new FSharpGeneratedMethod(this, GetHashCodeName, intType, isOverride: true));
      members.Add(new FSharpGeneratedMethod(this, GetHashCodeName, eqCompType, CompName, intType, isOverride: true));

      if (OverridesCompareTo)
      {
        members.Add(new FSharpGeneratedMethod(this, CompareToName, thisType, ObjName, boolType));
        members.Add(new FSharpGeneratedMethod(this, CompareToName, objType, ObjName, boolType));
        members.Add(new FSharpGeneratedMethod(this, CompareToName, objType, ObjName, compType, CompName, boolType));
      }

      if (OverridesToString)
        members.Add(new FSharpGeneratedMethod(this, ToStringName, stringType, isOverride: true));

      var baseMembers = base.GetMembers().AsIList();
      
      if (EmitsFieldsConstructor)
      {
        var fields = baseMembers.OfType<FSharpFieldProperty>().AsArray();
        if (!fields.IsEmpty())
          members.Add(new FSharpGeneratedConstructor(this, fields));
      }

      return baseMembers.Prepend(members.ResultingList());
    }
  }
}