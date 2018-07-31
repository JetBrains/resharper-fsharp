using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class GeneratedMembersUtil
  {
    public static IList<ITypeMember> GetGeneratedMembers(this TypePart typePart)
    {
      var typeElement = typePart.TypeElement;
      if (typeElement == null)
        return EmptyList<ITypeMember>.Instance;

      var result = new LocalList<ITypeMember>(new ITypeMember[]
      {
        new EqualsSimpleTypeMethod(typeElement),
        new EqualsObjectMethod(typeElement),
        new EqualsObjectWithComparerMethod(typeElement),

        new GetHashCodeMethod(typeElement),
        new GetHashCodeWithComparerMethod(typeElement),
      });

      if (typePart is ISimpleTypePart simpleTypePart)
      {
        if (simpleTypePart.OverridesToString)
          result.Add(new ToStringMethod(typeElement));

        if (simpleTypePart.HasCompareTo)
        {
          result.Add(new CompareToSimpleTypeMethod(typeElement));
          result.Add(new CompareToObjectMethod(typeElement));
          result.Add(new CompareToObjectWithComparerMethod(typeElement));
        }
      }

      switch (typePart)
      {
        case IRecordPart recordPart:
          result.Add(new FSharpGeneratedConstructorFromFields(typePart, recordPart.Fields));
          if (recordPart.CliMutable)
            result.Add(new DefaultConstructor(typeElement));
          break;

        case IExceptionPart exceptionPart:
          result.Add(new ExceptionConstructor(typePart));
          var fields = exceptionPart.Fields;
          if (!fields.IsEmpty())
            result.Add(new FSharpGeneratedConstructorFromFields(typePart, fields));
          break;
      }

      return result.ResultingList();
    }
  }
}
