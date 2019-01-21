using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class FSharpTypeParametersOwnerPart<T> : FSharpTypePart<T> where T : class, IFSharpTypeDeclaration
  {
    private readonly string[] myTypeParameterNames;

    protected FSharpTypeParametersOwnerPart([NotNull] T declaration, MemberDecoration memberDecoration,
      TreeNodeCollection<ITypeParameterOfTypeDeclaration> typeParameters, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.ShortName), memberDecoration, typeParameters.Count, cacheBuilder)
    {
      var parameters = declaration.TypeParameters;
      if (parameters.Count == 0)
      {
        myTypeParameterNames = EmptyArray<string>.Instance;
        return;
      }

      myTypeParameterNames = new string[typeParameters.Count];
      for (var i = 0; i < typeParameters.Count; i++)
        myTypeParameterNames[i] = cacheBuilder.Intern(typeParameters[i].DeclaredName);
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

    protected override string PrintTypeParameters() =>
      myTypeParameterNames.Length == 0
        ? ""
        : "<" + StringUtil.StringArrayText(myTypeParameterNames) + ">";
  }
}