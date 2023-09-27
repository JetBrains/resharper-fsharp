using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpProjectFilePart : SimpleProjectFilePart
  {
    public FSharpFileKind FileKind { get; }
    public bool HasPairFile { get; } // todo: rename HasPairFile to something better
    internal bool HasInternalsVisibleTo { get; set; }

    public FSharpProjectFilePart(IPsiSourceFile sourceFile, FSharpFileKind fileKind, bool hasPairFile)
      : base(sourceFile)
    {
      FileKind = fileKind;
      HasPairFile = hasPairFile;
    }

    public FSharpProjectFilePart(IPsiSourceFile sourceFile, IReader reader, FSharpFileKind fileKind)
      : base(sourceFile, reader)
    {
      FileKind = fileKind;

      HasPairFile = reader.ReadBool();
      HasInternalsVisibleTo = reader.ReadBool();
    }

    protected override void Write(IWriter writer)
    {
      writer.WriteBool(HasPairFile);
      writer.WriteBool(HasInternalsVisibleTo);
    }

    public bool IsSignature => FileKind == FSharpFileKind.SigFile;
    public bool IsImplementation => FileKind == FSharpFileKind.ImplFile;

    public override string ToString() => $"{GetType().Name}:{FileKind}";

    public override IList<IAttributeInstance> GetAttributeInstances()
    {
      if (!(GetFile() is IFSharpFile fsFile))
        return EmptyList<IAttributeInstance>.Instance;

      var result = new List<IAttributeInstance>();
      fsFile.Accept(new InternalsVisibleToProcessor(), result);
      return result;
    }

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName) =>
      HasInternalsVisibleTo && clrName.ShortName == "InternalsVisibleToAttribute"
        ? GetAttributeInstances()
        : EmptyList<IAttributeInstance>.Instance;

    public override string[] AttributeClassNames =>
      HasInternalsVisibleTo
        ? new[] {"InternalsVisibleTo"}
        : EmptyArray<string>.Instance;
  }

  internal class InternalsVisibleToProcessor : TreeNodeVisitor<List<IAttributeInstance>>
  {
    public override void VisitFSharpFile(IFSharpFile fsFile, List<IAttributeInstance> result)
    {
      foreach (var declaration in fsFile.ModuleDeclarations)
        declaration.Accept(this, result);
    }

    public override void VisitNamedNamespaceDeclaration(INamedNamespaceDeclaration moduleDecl,
      List<IAttributeInstance> context) =>
      ProcessModuleLikeDeclaration(moduleDecl, context);

    public override void VisitGlobalNamespaceDeclaration(IGlobalNamespaceDeclaration moduleDecl,
      List<IAttributeInstance> context) =>
      ProcessModuleLikeDeclaration(moduleDecl, context);

    public override void VisitNamedModuleDeclaration(INamedModuleDeclaration moduleDecl,
      List<IAttributeInstance> context) =>
      ProcessModuleLikeDeclaration(moduleDecl, context);

    public override void VisitAnonModuleDeclaration(IAnonModuleDeclaration moduleDecl,
      List<IAttributeInstance> context) =>
      ProcessModuleLikeDeclaration(moduleDecl, context);

    public override void VisitNestedModuleDeclaration(INestedModuleDeclaration moduleDecl,
      List<IAttributeInstance> context) =>
      ProcessModuleLikeDeclaration(moduleDecl, context);

    private void ProcessModuleLikeDeclaration(IModuleLikeDeclaration decl, List<IAttributeInstance> result)
    {
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this, result);
    }

    public override void VisitDoStatement(IDoStatement doStmt, List<IAttributeInstance> result) =>
      ProcessDoLikeStatement(doStmt, result);

    public override void VisitExpressionStatement(IExpressionStatement exprStmt, List<IAttributeInstance> result) =>
      ProcessDoLikeStatement(exprStmt, result);

    private void ProcessDoLikeStatement(IDoLikeStatement doStmt, List<IAttributeInstance> result)
    {
      foreach (var attribute in doStmt.Attributes)
        if (attribute.ReferenceName.ShortName.DropAttributeSuffix() == "InternalsVisibleTo")
          result.Add(new InternalsVisibleToAttributeInstance(attribute));
    }
  }

  /// Workaround for providing IVT attributes until attributes can be resolved in a better/faster way.
  internal class InternalsVisibleToAttributeInstance : IAttributeInstance
  {
    [CanBeNull] private readonly IFSharpExpression myArgExpr;

    public InternalsVisibleToAttributeInstance([NotNull] IAttribute attribute)
    {
      myArgExpr = attribute.Expression;
    }

    public AttributeValue PositionParameter(int paramIndex) =>
      PositionParameters().ToList()[paramIndex];

    public int PositionParameterCount => PositionParameters().Count();

    public IEnumerable<AttributeValue> PositionParameters()
    {
      var args = myArgExpr.IgnoreInnerParens() switch
      {
        ITupleExpr tupleExpr => tupleExpr.ExpressionsEnumerable.Select(expr => expr.IgnoreInnerParens()),
        var expr => new[] {expr}
      };

      return args.Select(expr =>
      {
        var value = expr is ILiteralExpr {FirstChild: FSharpString fsString}
          ? fsString.ConstantValue
          : ConstantValue.BAD_VALUE;

        return new AttributeValue(value);
      });
    }

    public IConstructor Constructor => null;
    public int NamedParameterCount => 0;

    public AttributeValue NamedParameter(string name) => throw new NotImplementedException();
    public IEnumerable<Pair<string, AttributeValue>> NamedParameters() => throw new NotImplementedException();

    public IClrTypeName GetClrName() => throw new NotImplementedException();
    public string GetAttributeShortName() => throw new NotImplementedException();
    public IDeclaredType GetAttributeType() => throw new NotImplementedException();
  }
}
