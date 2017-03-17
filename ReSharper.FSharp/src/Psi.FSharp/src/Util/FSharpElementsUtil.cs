using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public class FSharpElementsUtil
  {
    [CanBeNull]
    public static IDeclaredType GetBaseType([NotNull] FSharpEntity entity, [NotNull] IPsiModule psiModule)
    {
      return entity.BaseType != null ? GetDeclaredType(entity.BaseType.Value, psiModule) : null;
    }

    [NotNull]
    public static IEnumerable<IDeclaredType> GetSuperTypes([NotNull] FSharpEntity entity,
      [NotNull] IPsiModule psiModule)
    {
      var interfaces = entity.DeclaredInterfaces;
      var types = new List<IDeclaredType>(interfaces.Count + 1);
      foreach (var entityInterface in interfaces)
      {
        var declaredType = GetDeclaredType(entityInterface, psiModule);
        if (declaredType != null) types.Add(declaredType);
      }
      var baseType = GetBaseType(entity, psiModule);
      if (baseType != null) types.Add(baseType);
      return types;
    }

    [CanBeNull]
    public static string GetQualifiedName([NotNull] FSharpEntity entity)
    {
      // sometimes name includes assembly name, public key, etc and separated with comma
      return entity.QualifiedName.SubstringBefore(",");
    }

    [CanBeNull]
    public static IDeclaredType GetDeclaredType([NotNull] FSharpType type, [NotNull] IPsiModule psiModule)
    {
      if (type.IsGenericParameter) return null; // todo

      while (type.IsAbbreviation) type = type.AbbreviatedType;
      var qualifiedName = GetQualifiedName(type.TypeDefinition);
      if (qualifiedName == null) return null;
      var typeElement = TypeFactory.CreateTypeByCLRName(qualifiedName, psiModule);

      var args = type.GenericArguments;
      if (args.Count == 0) return typeElement;
      var typeArgs = new IType[args.Count];
      for (var i = 0; i < args.Count; i++)
      {
        var argType = GetDeclaredType(args[i], psiModule);
        if (argType == null) return null;

        typeArgs[i] = argType;
      }

      var element = typeElement.GetTypeElement();
      return element != null ? TypeFactory.CreateType(element, typeArgs) : null;
    }

    [CanBeNull]
    public static IDeclaredType GetDeclaredType([NotNull] FSharpEntity entity, [NotNull] IPsiModule psiModule)
    {
      if (((FSharpSymbol) entity).DeclarationLocation == null) return null;

      if (entity.IsFSharpAbbreviation)
      {
        var type = entity.AbbreviatedType;
        while (type.IsAbbreviation) type = type.AbbreviatedType;
        entity = type.TypeDefinition;
      }
      var qualifiedName = GetQualifiedName(entity);
      return qualifiedName != null ? TypeFactory.CreateTypeByCLRName(qualifiedName, psiModule) : null;
    }

    public static INamespace GetDeclaredNamespace([NotNull] FSharpEntity entity, IPsiModule psiModule)
    {
      var nsName = entity.CompiledName;
      var containingName = entity.Namespace?.Value;
      var fullName = containingName != null ? containingName + "." + nsName : nsName;
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
        return GetDeclaredType(entity, psiModule)?.GetTypeElement();
      }

      var unionCase = symbol as FSharpUnionCase;
      if (unionCase != null)
      {
        // here are two distinct cases:
        // * case with fields, inherited class
        // * case without fields, singleton property
      }

      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        try
        {
          var type = GetContainingType(mfv, psiModule);
          var members = type?.GetTypeElement()?.EnumerateMembers(mfv.CompiledName, true);
          return members?.FirstOrDefault(); // todo: check overloads
        }
        catch (Exception) // todo: remove this check and find element properly
        {
          return null;
        }
      }
      return null;
    }

    [CanBeNull]
    private static IDeclaredType GetContainingType([NotNull] FSharpMemberOrFunctionOrValue mfv,
      [NotNull] IPsiModule psiModule)
    {
      try
      {
        return GetDeclaredType(mfv.EnclosingEntity, psiModule);
      }
      catch (InvalidOperationException)
      {
        // element is local to some member and is not stored in R# caches
        return null;
      }
    }

    [CanBeNull]
    public static FSharpSymbol GetFSharpSymbol(IDeclaredElement element)
    {
      return (element as FSharpFakeElementFromReference)?.Symbol;
    }
  }
}