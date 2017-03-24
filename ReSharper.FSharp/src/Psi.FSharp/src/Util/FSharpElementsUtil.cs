using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  /// <summary>
  /// Map FSharpSymbol elements (as seen by FSharp.Compiler.Service) to declared elements.
  /// </summary>
  public class FSharpElementsUtil
  {
    [CanBeNull]
    private static ITypeElement GetTypeElement([NotNull] FSharpEntity entity, [NotNull] IPsiModule psiModule)
    {
      if (((FSharpSymbol) entity).DeclarationLocation == null)
        return null;

      if (!entity.IsFSharpAbbreviation)
        return TypeFactory.CreateTypeByCLRName(FSharpTypesUtil.GetClrName(entity), psiModule).GetTypeElement();

      var symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, true, true);
      while (entity.IsFSharpAbbreviation)
      {
        // it's easier to use qualified name with abbreviations
        // FCS can return CLR names for non-abbreviated types only

        var qualifiedName = ((FSharpSymbol) entity).FullName;
        var typeElement = symbolScope.GetElementsByQualifiedName(qualifiedName).FirstOrDefault() as ITypeElement;
        if (typeElement != null)
          return typeElement;

        entity = entity.AbbreviatedType.TypeDefinition;
      }

      return TypeFactory.CreateTypeByCLRName(FSharpTypesUtil.GetClrName(entity), psiModule).GetTypeElement();
    }

    [CanBeNull]
    private static INamespace GetDeclaredNamespace([NotNull] FSharpEntity entity, IPsiModule psiModule)
    {
      Assertion.Assert(entity.IsNamespace, "entity.IsNamespace");
      var name = entity.CompiledName;
      var containingName = entity.Namespace?.Value;
      var fullName = containingName != null ? containingName + "." + name : name;
      var symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, true, true);
      return symbolScope.GetElementsByQualifiedName(fullName).FirstOrDefault() as INamespace;
    }

    [CanBeNull]
    public static IClrDeclaredElement GetDeclaredElement([NotNull] FSharpSymbol symbol, [NotNull] IPsiModule psiModule)
    {
      var entity = symbol as FSharpEntity;
      if (entity != null)
      {
        if (entity.IsNamespace) return GetDeclaredNamespace(entity, psiModule);
        return GetTypeElement(entity, psiModule);
      }

      var unionCase = symbol as FSharpUnionCase;
      if (unionCase != null)
      {
        // here are two distinct cases:
        // * case with fields, inherited class
        // * case without fields, singleton property
      }

      var field = symbol as FSharpField;
      if (field != null)
      {
        var typeElement = GetTypeElement(field.DeclaringEntity, psiModule);
        return typeElement?.EnumerateMembers(field.Name, true).FirstOrDefault();
      }

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        var memberEntity = GetContainingEntity(mfv);
        if (memberEntity == null)
          return null;

        var typeElement = GetTypeElement(memberEntity, psiModule);
        if (typeElement == null)
          return null;

        var fsMember = GetMemberWithoutSubstitution(mfv, memberEntity);
        if (fsMember == null)
          return null;

        var typeParameters = typeElement.GetAllTypeParameters().ToIList();
        var members = typeElement.EnumerateMembers(mfv.CompiledName, true);
        return members.FirstOrDefault(m => SameParameters(m, fsMember, typeParameters, psiModule));
      }
      return null;
    }

    private static bool SameParameters([NotNull] ITypeMember member, [NotNull] FSharpMemberOrFunctionOrValue mfv,
      IList<ITypeParameter> typeParameters, IPsiModule psiModule)
    {
      var paramsOwner = member as IParametersOwner;
      if (paramsOwner == null)
        return true;

      var typeParametersOwner = member as ITypeParametersOwner;
      var methodTypeParameters = typeParametersOwner != null
        ? typeParameters.Prepend(typeParametersOwner.TypeParameters).ToIList()
        : typeParameters;

      var memberParams = paramsOwner.Parameters.ToArray();
      var fsParamsTypes = GetParametersTypes(mfv);
      if (memberParams.Length != fsParamsTypes.Length)
        return false;

      for (var i = 0; i < fsParamsTypes.Length; i++)
      {
        var memberParamType = memberParams[i].Type;
        var fsParamType = fsParamsTypes[i];

        var typeParameter = memberParamType.GetTypeElement() as ITypeParameter;
        if (typeParameter != null)
        {
          if (!fsParamType.IsGenericParameter)
            return false;

          var typeParameterName = typeParameter.ShortName;
          var fsTypeParameterName = fsParamType.GenericParameter.Name;
          if (typeParameterName != fsTypeParameterName &&
              (typeParameterName[0] != 'T' || typeParameterName.Substring(1) != fsTypeParameterName))
            return false; // sometimes TKey replaced with Key, todo: why and where?
        }
        else
        {
          if (fsParamType.IsGenericParameter)
            return false;

          if (!memberParamType.Equals(FSharpTypesUtil.GetType(fsParamType, methodTypeParameters, psiModule)))
            return false;
        }
      }

      return true;
    }

    private static FSharpType[] GetParametersTypes([NotNull] FSharpMemberOrFunctionOrValue mfv)
    {
      var result = new List<FSharpType>();
      foreach (var paramGroup in mfv.CurriedParameterGroups)
        result.AddRange(paramGroup.Select(param => param.Type));
      return result.ToArray();
    }

    [CanBeNull]
    private static FSharpEntity GetContainingEntity([NotNull] FSharpMemberOrFunctionOrValue mfv)
    {
      try
      {
        return mfv.EnclosingEntity;
      }
      catch (InvalidOperationException)
      {
        // element is local to some member and is not stored in R# caches
        return null;
      }
    }

    [CanBeNull]
    private static FSharpMemberOrFunctionOrValue GetMemberWithoutSubstitution([NotNull] FSharpSymbol mfv,
      [NotNull] FSharpEntity entity)
    {
      return entity.MembersFunctionsAndValues.FirstOrDefault(m => m.IsEffectivelySameAs(mfv));
    }

    [CanBeNull]
    public static FSharpSymbol GetFSharpSymbolFromFakeElement(IDeclaredElement element)
    {
      return (element as FSharpFakeElementFromReference)?.Symbol;
    }
  }
}