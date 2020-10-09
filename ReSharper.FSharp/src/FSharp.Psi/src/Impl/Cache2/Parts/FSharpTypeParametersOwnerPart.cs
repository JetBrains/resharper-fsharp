using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class FSharpTypeParametersOwnerPart<T> : FSharpTypePart<T> where T : class, IFSharpTypeOldDeclaration
  {
    private readonly string[] myTypeParameterNames;

    protected FSharpTypeParametersOwnerPart([NotNull] T declaration, MemberDecoration memberDecoration,
      TreeNodeCollection<ITypeParameterOfTypeDeclaration> typeParameters, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.CompiledName), memberDecoration, typeParameters.Count,
        cacheBuilder)
    {
      if (typeParameters.Count == 0)
      {
        myTypeParameterNames = EmptyArray<string>.Instance;
        return;
      }

      myTypeParameterNames = new string[typeParameters.Count];
      for (var i = 0; i < typeParameters.Count; i++)
        myTypeParameterNames[i] = cacheBuilder.Intern(typeParameters[i].CompiledName);
    }

    protected FSharpTypeParametersOwnerPart(IReader reader) : base(reader)
    {
      var number = TypeParameterNumber;
      if (number == 0)
      {
        myTypeParameterNames = EmptyArray<string>.Instance;
        return;
      }

      myTypeParameterNames = new string[number];
      for (var index = 0; index < number; index++)
        myTypeParameterNames[index] = reader.ReadString();
    }

    protected override void AssignDeclaredElement(ICachedDeclaration2 declaration)
    {
      base.AssignDeclaredElement(declaration);

      var parameters = TypeElement?.TypeParameters;
      if (parameters == null || parameters.IsEmpty())
        return;

      var typeDeclaration = (T) declaration;
      var typeParameterDeclarations = typeDeclaration.TypeParameters;
      var parametersCount = Math.Min(parameters.Count, typeParameterDeclarations.Count);

      for (var i = 0; i < parametersCount; i++)
        AssignToCachedDeclaration((ICachedDeclaration2) typeParameterDeclarations[i], parameters[i]);
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      foreach (var parameterName in myTypeParameterNames)
        writer.WriteString(parameterName);
    }

    public override IDeclaration GetTypeParameterDeclaration(int index) =>
      index < TypeParameterNumber && GetDeclaration() is IFSharpTypeOldDeclaration declaration
        ? declaration.TypeParameters[index]
        : null;

    public override string GetTypeParameterName(int index) =>
      myTypeParameterNames[index];

    public override TypeParameterVariance GetTypeParameterVariance(int index) =>
      TypeParameterVariance.INVARIANT;

    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index) =>
      EmptyList<IType>.Instance; // todo

    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index) => 0; // todo

    protected override string PrintTypeParameters() =>
      myTypeParameterNames.Length == 0
        ? ""
        : "<" + StringUtil.StringArrayText(myTypeParameterNames) + ">";
  }
}
