using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ExceptionDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => Identifier;
    public bool HasFields => !FieldsEnumerable.IsEmpty();
    public FSharpUnionCaseClass NestedType => null;
    public IFSharpParameterDeclaration GetParameterDeclaration(FSharpParameterIndex index) => null;

    public IList<IList<IFSharpParameterDeclaration>> GetParameterDeclarations() =>
      EmptyList<IList<IFSharpParameterDeclaration>>.Instance;

    public void SetParameterFcsType(FSharpParameterIndex index, FSharpType fcsType) =>
      this.SetFieldDeclFcsType(index, fcsType);
  }
}
