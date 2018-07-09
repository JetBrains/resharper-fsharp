using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class UnionPart : SimpleTypePartBase, Class.IClassPart
  {
    public UnionPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public UnionPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpUnion(this);
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Union;

    public IList<ITypeMember> Cases
    {
      get
      {
        var declaration = GetDeclaration();
        if (declaration == null)
          return EmptyList<ITypeMember>.Instance;

        var result = new LocalList<ITypeMember>();
        foreach (var memberDeclaration in declaration.MemberDeclarations)
        {
          if (memberDeclaration is IUnionCaseDeclaration)
          {
            var unionCase = memberDeclaration.DeclaredElement;
            if (unionCase != null)
              result.Add(unionCase);
            continue;
          }

          if (memberDeclaration is FieldDeclaration)
          {
            var declaredElement = memberDeclaration.DeclaredElement;
            if (declaredElement is FSharpUnionCaseProperty)
              result.Add(declaredElement);
          }
        }

        return result.ResultingList();
      }
    }
  }
}