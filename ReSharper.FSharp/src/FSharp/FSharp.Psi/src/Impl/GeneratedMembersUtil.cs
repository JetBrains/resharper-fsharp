using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class GeneratedMembersUtil
  {
    public static IList<ITypeMember> GetGeneratedMembers(this TypePart typePart)
    {
      if (typePart.TypeElement is not { } typeElement)
        return EmptyList<ITypeMember>.Instance;

      var result = new LocalList<ITypeMember>(new ITypeMember[]
      {
        new EqualsStructuralTypeMethod(typeElement),
        new EqualsObjectMethod(typeElement),
        new EqualsObjectWithComparerMethod(typeElement),

        new GetHashCodeMethod(typeElement),
        new GetHashCodeWithComparerMethod(typeElement),
      });

      if (typePart is IFSharpStructuralTypePart simpleTypePart)
      {
        if (simpleTypePart.OverridesToString)
          result.Add(new ToStringMethod(typeElement));

        if (simpleTypePart.HasCompareTo)
        {
          result.Add(new CompareToStructuralTypeMethod(typeElement));
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

        case IFSharpExceptionPart exceptionPart:
          result.Add(new FSharpGeneratedExceptionDefaultConstructor(typePart));
          if (exceptionPart.HasFields)
            result.Add(new FSharpGeneratedConstructorFromFields(typePart));
          break;

        case IUnionPart unionPart:
          result.Add(new UnionTagProperty(typeElement));
          foreach (var unionCase in unionPart.Cases)
          {
            if (unionCase.HasFields)
            {
              result.Add(new FSharpUnionCaseNewMethod(unionCase));

              if (!unionPart.HasNestedTypes)
                result.AddRange(unionCase.CaseFields);
              else if (unionCase.NestedType is { } nestedType)
                result.Add(nestedType);
            }

            if (!unionPart.IsSingleCase)
              result.Add(new FSharpUnionCaseIsCaseProperty(unionCase));
          }

          if (!unionPart.IsSingleCase)
            result.Add(new FSharpUnionTagsClass(typeElement));
          break;
      }

      return result.ResultingList();
    }

    public static IEnumerable<IDeclaredElement> GetGeneratedMembers([NotNull] this IFSharpUnionCase unionCase)
    {
      if (unionCase.ContainingType.GetPart<IUnionPart>() is not { } unionPart)
        return EmptyList<IDeclaredElement>.Instance;

      if (unionPart.IsSingleCase && !unionCase.HasFields)
        return EmptyList<IDeclaredElement>.Instance;

      var result = new List<IDeclaredElement>();
      if (unionCase.HasFields)
      {
        result.Add(new FSharpUnionCaseNewMethod(unionCase));

        if (unionCase.NestedType is { } nestedType)
          result.Add(nestedType);
      }

      if (unionPart.IsSingleCase)
        return result;

      result.Add(new FSharpUnionCaseIsCaseProperty(unionCase));
      result.Add(new FSharpUnionCaseTag(unionCase));

      return result;
    }

    public static IEnumerable<IDeclaredElement> GetCompiledUnionCaseGeneratedMembers(
      [NotNull] ITypeElement typeElement, string caseName)
    {
      var result = new List<IDeclaredElement>();

      result.AddRange(typeElement.EnumerateMembers(caseName, true));
      result.AddRange(typeElement.EnumerateMembers("Is" + caseName, true).Where(member => member is IProperty));
      result.AddRange(typeElement.EnumerateMembers("New" + caseName, true).Where(member => member is IMethod));

      if (typeElement.NestedTypes.FirstOrDefault(element => element.ShortName == "Tags") is IClass tagsClass)
        result.AddRange(tagsClass.EnumerateMembers(caseName, true));

      if (typeElement.NestedTypes.FirstOrDefault(element => element.ShortName == caseName) is IClass caseClass)
        result.Add(caseClass);

      return result;
    }
  }
}
