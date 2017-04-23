using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.FSharp.Impl;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  /// <summary>
  /// Map FSharpSymbol elements (as seen by FSharp.Compiler.Service) to declared elements.
  /// </summary>
  public static class FSharpElementsUtil
  {
    [CanBeNull]
    private static ITypeElement GetTypeElement([NotNull] FSharpEntity entity, [NotNull] IPsiModule psiModule)
    {
      if (((FSharpSymbol) entity).DeclarationLocation == null || entity.IsByRef || entity.IsProvidedAndErased)
        return null;

      if (!entity.IsFSharpAbbreviation)
        return TypeFactory.CreateTypeByCLRName(FSharpTypesUtil.GetClrName(entity), psiModule).GetTypeElement();

      var symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, true, true);
      while (entity.IsFSharpAbbreviation)
      {
        // FCS returns Clr names for non-abbreviated types only, using fullname
        var typeElement = TryFindByNames(GetPossibleNames(entity), symbolScope);
        if (typeElement != null)
          return typeElement;

        var abbreviatedType = entity.AbbreviatedType;
        if (!abbreviatedType.HasTypeDefinition)
          return null;

        entity = entity.AbbreviatedType.TypeDefinition;
      }

      return TypeFactory.CreateTypeByCLRName(FSharpTypesUtil.GetClrName(entity), psiModule).GetTypeElement();
    }

    private static IEnumerable<string> GetPossibleNames([NotNull] FSharpEntity entity)
    {
      yield return entity.AccessPath + "." + entity.DisplayName;
      yield return entity.AccessPath + "." + entity.LogicalName;
      yield return ((FSharpSymbol) entity).FullName;
    }

    [CanBeNull]
    private static ITypeElement TryFindByNames([NotNull] IEnumerable<string> names, ISymbolScope symbolScope)
    {
      foreach (var name in names)
      {
        var typeElement = symbolScope.GetElementsByQualifiedName(name).FirstOrDefault() as ITypeElement;
        if (typeElement != null)
          return typeElement;
      }
      return null;
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
    public static IClrDeclaredElement GetDeclaredElement([CanBeNull] FSharpSymbol symbol,
      [NotNull] IPsiModule psiModule, [CanBeNull] IFSharpFile fsFile = null)
    {
      if (symbol == null)
        return null;

      var entity = symbol as FSharpEntity;
      if (entity != null)
        return entity.IsNamespace
          ? (IClrDeclaredElement) GetDeclaredNamespace(entity, psiModule)
          : GetTypeElement(entity, psiModule);

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        if (!mfv.IsModuleValueOrMember && fsFile != null)
          return FindLocalDeclaration(mfv, fsFile);

        var memberEntity = GetContainingEntity(mfv);
        if (memberEntity == null)
          return null;

        if (mfv.IsImplicitConstructor)
          return GetDeclaredElement(memberEntity, psiModule);

        var typeElement = GetTypeElement(memberEntity, psiModule);
        if (typeElement == null)
          return null;

        if (mfv.IsProperty)
          return typeElement.EnumerateMembers(mfv.GetMemberCompiledName(), true).FirstOrDefault();

        var fsMember = GetMemberWithoutSubstitution(mfv, memberEntity);
        if (fsMember == null)
          return null;

        var typeParameters = typeElement.GetAllTypeParameters().ToIList();
        var members = mfv.IsConstructor
          ? typeElement.Constructors
          : typeElement.EnumerateMembers(mfv.CompiledName, true);

        var member = members.FirstOrDefault(m => SameParameters(m, fsMember, typeParameters, psiModule));
        return member ??
               // todo: rarely happens, couldn't map types (with substitutions), needs a proper fix
               typeElement.EnumerateMembers(mfv.CompiledName, true).FirstOrDefault();
      }

      var unionCase = symbol as FSharpUnionCase;
      if (unionCase != null)
      {
        var unionType = unionCase.ReturnType;
        Assertion.AssertNotNull(unionType, "unionType != null");
        var unionTypeElement = GetTypeElement(unionType.TypeDefinition, psiModule);
        if (unionTypeElement == null)
          return null;

        var caseMember = unionTypeElement.EnumerateMembers(unionCase.CompiledName, true).FirstOrDefault();
        if (caseMember != null)
          return caseMember;

        var newCaseMember = unionTypeElement.EnumerateMembers("New" + unionCase.CompiledName, true).FirstOrDefault();
        if (newCaseMember != null)
          return newCaseMember;

        var unionClrName = unionTypeElement.GetClrName();
        var caseDeclaredType = TypeFactory.CreateTypeByCLRName(unionClrName + "+" + unionCase.CompiledName, psiModule);
        return caseDeclaredType.GetTypeElement();
      }

      var field = symbol as FSharpField;
      if (field != null)
      {
        var typeElement = GetTypeElement(field.DeclaringEntity, psiModule);
        return typeElement?.EnumerateMembers(field.Name, true).FirstOrDefault();
      }

      return null;
    }

    private static IClrDeclaredElement FindLocalDeclaration([NotNull] FSharpMemberOrFunctionOrValue mfv,
      [NotNull] IFSharpFile fsFile)
    {
      var declRange = mfv.DeclarationLocation;
      var document = fsFile.GetSourceFile()?.Document;
      if (document == null)
        return null;

      var idToken = fsFile.FindTokenAt(document.GetTreeEndOffset(declRange) - 1);
      return idToken?.GetContainingNode<LocalDeclaration>();
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
      return mfv.IsModuleValueOrMember ? mfv.EnclosingEntity : null;
    }

    [CanBeNull]
    private static FSharpMemberOrFunctionOrValue GetMemberWithoutSubstitution(
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [NotNull] FSharpEntity entity)
    {
      var sameMfv = entity.MembersFunctionsAndValues.FirstOrDefault(m => m.IsEffectivelySameAs(mfv));
      if (sameMfv != null)
        return sameMfv;

      if (mfv.IsConstructor)
      {
        // bug in FCS, https://github.com/fsharp/FSharp.Compiler.Service/issues/752
        var mfvParamsCount = mfv.ParametersCount();
        return entity.MembersFunctionsAndValues.FirstOrDefault(
          m => m.CompiledName == ".ctor" && m.ParametersCount() == mfvParamsCount);
      }
      return null;
    }

    private static int ParametersCount([NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      return mfv.CurriedParameterGroups.SelectMany(p => p).ToList().Count;
    }
  }
}