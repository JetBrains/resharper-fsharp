using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public class FSharpElementsUtil
  {
    [CanBeNull]
    public static IClrDeclaredElement GetDeclaredElement([NotNull] FSharpSymbol symbol, [NotNull] IPsiModule psiModule)
    {
      // todo: map symbols to R# elements
      return null;
    }

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
    public static IDeclaredType GetDeclaredType([NotNull] FSharpType type, [NotNull] IPsiModule psiModule)
    {
      while (type.IsAbbreviation) type = type.AbbreviatedType;
      var typeDefinition = type.TypeDefinition;

      // sometimes name includes assembly name, public key, etc and separated with comma
      var qualifiedName = typeDefinition.QualifiedName.SubstringBefore(",");
      var typeElement = TypeFactory.CreateTypeByCLRName(qualifiedName, psiModule);

      var args = type.GenericArguments;
      if (args.Count == 0) return typeElement;
      var typeArgs = new IType[args.Count];
      for (var i = 0; i < args.Count; i++)
        typeArgs[i] = GetDeclaredType(args[i], psiModule);
      var element = typeElement.GetTypeElement();
      return element != null ? TypeFactory.CreateType(element, typeArgs) : null;
    }
  }
}