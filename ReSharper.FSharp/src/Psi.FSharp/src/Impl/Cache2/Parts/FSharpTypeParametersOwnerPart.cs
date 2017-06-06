using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts
{
  internal abstract class FSharpTypeParametersOwnerPart<T> : FSharpTypePart<T>
    where T : class, IFSharpTypeDeclaration, ITypeDeclaration
  {
    private readonly string[] myTypeParameterNames;

    protected FSharpTypeParametersOwnerPart(T declaration, MemberDecoration memberDecoration,
      TreeNodeCollection<ITypeParameterOfTypeDeclaration> typeParameters, ICacheBuilder cacheBuilder)
      : base(declaration, memberDecoration, typeParameters.Count, cacheBuilder)
    {
      var parameters = declaration.TypeParameters;
      if (parameters.Count == 0)
      {
        myTypeParameterNames = EmptyArray<string>.Instance;
        return;
      }

      myTypeParameterNames = new string[typeParameters.Count];
      for (var i = 0; i < typeParameters.Count; i++)
      {
        var name = typeParameters[i].GetText();
        var trimmed = name[0] == '\'' ? name.Substring(1) : name;
        myTypeParameterNames[i] = cacheBuilder.Intern(trimmed);
      }
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

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      foreach (var parameterName in myTypeParameterNames)
        writer.WriteString(parameterName);
    }

    public override IDeclaration GetTypeParameterDeclaration(int index)
    {
      var declaration = GetDeclaration() as IFSharpTypeDeclaration;
      return index < TypeParameterNumber ? declaration?.TypeParameters[index] : null;
    }

    public override string GetTypeParameterName(int index)
    {
      return myTypeParameterNames[index];
    }

    public override TypeParameterVariance GetTypeParameterVariance(int index)
    {
      return TypeParameterVariance.INVARIANT;
    }

    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index)
    {
      // todo
      return EmptyList<IType>.Instance;
    }

    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index)
    {
      return 0;
    }
  }
}