using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ObjExpr
  {
    public override IFSharpIdentifier NameIdentifier => TypeName.Identifier;

    protected override string DeclaredElementName =>
      GetSourceFile() is { } sourceFile && sourceFile.GetLocation() is var path && !path.IsEmpty
        ? "Object expression in " + path.Name + "@" + GetTreeStartOffset()
        : SharedImplUtil.MISSING_DECLARATION_NAME;

    public override string SourceName => SharedImplUtil.MISSING_DECLARATION_NAME;

    public bool IsConstantValue() => false;
    public ConstantValue ConstantValue => ConstantValue.BAD_VALUE;
    public ExpressionAccessType GetAccessType() => ExpressionAccessType.None;

    public IType Type() => this.GetExpressionTypeFromFcs();
    public IExpressionType GetExpressionType() => Type();
    public IType GetImplicitlyConvertedTo() => Type();

    public override IEnumerable<IDeclaredType> SuperTypes
    {
      get
      {
        var mainTypeReference = TypeName?.Reference;
        var result = new List<IDeclaredType>();

        if (mainTypeReference?.ResolveType() is { } superClassOrInterface)
          result.Add(superClassOrInterface);

        foreach (var interfaceImpl in InterfaceImplementations)
          if (interfaceImpl.TypeName?.Reference.ResolveType() is { } secondaryType && secondaryType.IsInterfaceType())
            result.Add(secondaryType);

        return result;
      }
    }

    public override IDeclaredType BaseClassType =>
      TypeName?.Reference.ResolveType();

    public override FSharpSymbol GetFcsSymbol() =>
      TypeName?.Reference.GetFcsSymbol();
  }
}
