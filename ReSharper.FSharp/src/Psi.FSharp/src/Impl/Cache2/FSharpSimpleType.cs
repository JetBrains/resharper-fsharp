using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpSimpleType<TPart> : FSharpClassLikeElement<TPart> where TPart : Part
  {
    private const string CompareToName = "CompareTo";
    private const string EqualsName = "Equals";
    private const string GetHashCodeName = "GetHashCode";
    private const string ObjName = "obj";
    private const string CompName = "comp";
    private const string ComparerTypeName = "System.Collections.IComparer";
    private const string EqComparerTypeName = "System.Collections.IEqualityComparer";

    public FSharpSimpleType([NotNull] IClassPart part) : base(part)
    {
    }

    public override IEnumerable<ITypeMember> GetMembers()
    {
      var boolType = Module.GetPredefinedType().Bool;
      var intType = Module.GetPredefinedType().Int;
      var objType = Module.GetPredefinedType().Object;
      var thisType = TypeFactory.CreateType(this);
      var compType = TypeFactory.CreateTypeByCLRName(ComparerTypeName, Module);
      var eqCompType = TypeFactory.CreateTypeByCLRName(EqComparerTypeName, Module);

      var members = new LocalList<ITypeMember>();

      members.Add(new FSharpGeneratedMethod(this, CompareToName, thisType, ObjName, boolType));
      members.Add(new FSharpGeneratedMethod(this, CompareToName, objType, ObjName, boolType));
      members.Add(new FSharpGeneratedMethod(this, CompareToName, objType, ObjName, compType, CompName, boolType));

      members.Add(new FSharpGeneratedMethod(this, EqualsName, thisType, ObjName, boolType));
      members.Add(new FSharpGeneratedMethod(this, EqualsName, objType, ObjName, boolType));
      members.Add(new FSharpGeneratedMethod(this, EqualsName, objType, ObjName, eqCompType, CompName, boolType));

      members.Add(new FSharpGeneratedMethod(this, GetHashCodeName, intType, true));
      members.Add(new FSharpGeneratedMethod(this, GetHashCodeName, eqCompType, CompName, intType, true));

      return base.GetMembers().Prepend(members.ResultingList());
    }
  }
}