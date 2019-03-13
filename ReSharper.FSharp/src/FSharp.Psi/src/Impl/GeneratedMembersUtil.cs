using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
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
      if (!(typePart.TypeElement is TypeElement typeElement))
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
          result.Add(new FSharpGeneratedConstructorFromFields(typePart));
          if (recordPart.CliMutable && typePart is Class.IClassPart)
            result.Add(new DefaultConstructor(typeElement));
          break;

        case IExceptionPart exceptionPart:
          result.Add(new ExceptionConstructor(typePart));
          if (exceptionPart.HasFields)
            result.Add(new FSharpGeneratedConstructorFromFields(typePart));
          break;

        case IUnionPart unionPart:
          result.Add(new UnionTagProperty(typeElement));
          foreach (var unionCase in unionPart.Cases)
          {
            if (unionCase is FSharpNestedTypeUnionCase typedCase)
            {
              result.Add(new NewUnionCaseMethod(typedCase));

              if (!unionPart.HasPublicNestedTypes)
                result.AddRange(typedCase.CaseFields);
            }

            if (!unionPart.IsSingleCaseUnion)
              result.Add(new IsUnionCaseProperty(unionCase));
          }

          if (!unionPart.IsSingleCaseUnion)
            result.Add(new FSharpUnionTagsClass(typeElement));
          break;
      }

      return result.ResultingList();
    }

    public static IEnumerable<IDeclaredElement> GetGeneratedMembers([NotNull] this IUnionCase unionCase)
    {
      if (!(unionCase.GetContainingType().GetUnionPart() is IUnionPart unionPart))
        return EmptyList<IDeclaredElement>.Instance;

      if (unionPart.IsSingleCaseUnion && unionCase is FSharpUnionCaseProperty)
        return EmptyList<IDeclaredElement>.Instance;

      var result = new List<IDeclaredElement>();
      if (unionCase is FSharpNestedTypeUnionCase)
        result.Add(new NewUnionCaseMethod(unionCase));

      if (unionPart.IsSingleCaseUnion)
        return result;

      result.Add(new IsUnionCaseProperty(unionCase));
      result.Add(new UnionCaseTag(unionCase));

      return result;
    }
  }
}
