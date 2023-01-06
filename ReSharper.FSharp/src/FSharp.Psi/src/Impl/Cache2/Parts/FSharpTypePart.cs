using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public abstract class FSharpTypePart<T> : TypePartImplBase<T>, IFSharpTypePart
    where T : class, IFSharpTypeElementDeclaration
  {
    public override ExtensionMethodInfo[] ExtensionMethodInfos { get; } = EmptyArray<ExtensionMethodInfo>.Instance;
    public string SourceName { get; }

    protected FSharpTypePart([NotNull] T declaration, [NotNull] string shortName, MemberDecoration memberDecoration,
      int typeParameters, [NotNull] ICacheBuilder cacheBuilder) : base(declaration, shortName, typeParameters)
    {
      Modifiers = memberDecoration;
      SourceName = cacheBuilder.Intern(declaration.SourceName);

      var attrNames = new FrugalLocalHashSet<string>();
      foreach (var attr in declaration.GetAttributes())
        attrNames.Add(cacheBuilder.Intern(attr.GetShortName()));
      AttributeClassNames = attrNames.ToArray();

      var methods = new LocalList<ExtensionMethodInfo>();
      foreach (var member in declaration.MemberDeclarations)
      {
        // There are two interesting scenarios:
        // * Members in types
        // * Bindings in modules
        // Type declaration as a member can only appear in module and we ignore it.
        if (member is ITypeDeclaration)
          continue;

        // A cheap check until we have a proper attributes resolve during cache building.
        if (!member.GetAttributes().Any(a => a.ShortNameEquals("Extension")))
          continue;

        var offset = member.GetTreeStartOffset().Offset;
        methods.Add(new ExtensionMethodInfo(AnyCandidateType.INSTANCE, offset, member.DeclaredName, this));
      }

      if (methods.IsEmpty())
        return;

      ExtensionMethodInfos = methods.ToArray();
    }

    [NotNull]
    public TypePart GetFirstPart()
    {
      var part = (TypePart)this;
      var offset = Offset;

      for (var nextPart = NextPart; nextPart != null; nextPart = nextPart.NextPart)
      {
        var filePart = (FSharpProjectFilePart)part.GetRoot();
        var nextPartFilePart = (FSharpProjectFilePart)nextPart.GetRoot();

        if (nextPart.Offset < offset && filePart == nextPartFilePart || 
            filePart.IsImplementation && nextPartFilePart.IsSignature)
        {
          part = nextPart;
          offset = nextPart.Offset;
        }
      }

      return part;
    }

    public override HybridCollection<IMethod> FindExtensionMethod(ExtensionMethodInfo info)
    {
      if (!TypeElement.HasAttributeInstance(PredefinedType.EXTENSION_ATTRIBUTE_CLASS, false))
        return HybridCollection<IMethod>.Empty;

      var declaration = GetDeclaration();
      if (declaration == null)
        return HybridCollection<IMethod>.Empty;

      var result = HybridCollection<IMethod>.Empty;
      foreach (var memberDeclaration in declaration.MemberDeclarations)
        if (info.ShortName == memberDeclaration.DeclaredName &&
            info.Hash == memberDeclaration.GetTreeStartOffset().Offset &&
            memberDeclaration.DeclaredElement is IMethod method)
          result = result.Add(method);

      return result;
    }

    protected FSharpTypePart(IReader reader) : base(reader)
    {
      SourceName = reader.ReadString();
      Modifiers = MemberDecoration.FromInt(reader.ReadInt());
      AttributeClassNames = reader.ReadStringArray();
      var extensionMethodCount = reader.ReadInt();
      if (extensionMethodCount <= 0)
        return;

      var methods = new ExtensionMethodInfo[extensionMethodCount];
      for (var i = 0; i < extensionMethodCount; i++)
        methods[i] = new ExtensionMethodInfo(reader, this);
      ExtensionMethodInfos = methods;
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteString(SourceName);
      writer.WriteInt(Modifiers.ToInt());
      writer.WriteStringArray(AttributeClassNames);

      writer.WriteInt(ExtensionMethodInfos.Length);
      foreach (var info in ExtensionMethodInfos)
        info.Write(writer);
    }

    protected override ICachedDeclaration2 FindDeclaration(IFile file, ICachedDeclaration2 candidateDeclaration) =>
      Offset >= TreeOffset.Zero && candidateDeclaration is T
        ? candidateDeclaration
        : null;

    public override string[] ExtendsListShortNames => EmptyArray<string>.Instance;
    public override MemberDecoration Modifiers { get; }

    /// Most F# elements are considered partial as an easy way
    /// to support signatures, intrinsic type extensions and virtual members.
    public override bool CanBePartial => true;

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName)
    {
      bool IsInDeclarationBounds(FSharpAttribute fcsAttribute, DocumentRange declarationRange) =>
        declarationRange.StartOffset.GetPos().Line <= fcsAttribute.Range.StartLine
        && declarationRange.StartOffset.GetPos().Column <= fcsAttribute.Range.StartColumn
        && declarationRange.EndOffset.GetPos().Line >= fcsAttribute.Range.EndLine
        && declarationRange.EndOffset.GetPos().Column >= fcsAttribute.Range.StartColumn;

      if (AttributeClassNames.IsEmpty())
        return EmptyList<IAttributeInstance>.Instance;

      var declaration = GetDeclaration();
      if (declaration?.GetFcsSymbol() is not FSharpEntity entity)
        return EmptyList<IAttributeInstance>.Instance;

      if (entity.Attributes.Count == 0)
        return EmptyList<IAttributeInstance>.Instance;

      Func<FSharpAttribute, bool> attributesFilter = clrName == null
        ? _ => true // If clrName is null we need to return all available attributes
        : fcsAttribute => new ClrTypeName(fcsAttribute.AttributeType.BasicQualifiedName).Equals(clrName);

      var declarationRange = declaration.GetDocumentRange();
      return entity.Attributes
        .Where(attributesFilter)
        .Where(fcsAttribute => IsInDeclarationBounds(fcsAttribute, declarationRange))
        .Select(fcsAttribute => new FSharpAttributeInstance(fcsAttribute, GetPsiModule()))
        .OfType<IAttributeInstance>()
        .ToList();
    }

    public override bool HasAttributeInstance(IClrTypeName clrTypeName)
    {
      if (AttributeClassNames.Contains(clrTypeName.ShortName))
        return false;

      // todo: get entity without getting declaration
      var entity = GetDeclaration()?.GetFcsSymbol() as FSharpEntity;
      return entity?.Attributes.HasAttributeInstance(clrTypeName.FullName) ?? false;
    }

    public override IList<IAttributeInstance> GetTypeParameterAttributeInstances(int index, IClrTypeName typeName) =>
      EmptyList<IAttributeInstance>.Instance; // todo

    public override bool HasTypeParameterAttributeInstance(int index, IClrTypeName typeName) =>
      false; // todo

    public override string[] AttributeClassNames { get; }

    public override string ToString()
    {
      var typeElement = TypeElement?.ToString() ?? "null";
      var typeParameters = PrintTypeParameters();

      return $"{GetType().Name}:{ShortName}{typeParameters}->{typeElement}";
    }

    protected virtual string PrintTypeParameters() => "";

    public virtual int MeasureTypeParametersCount => 0;
  }
}
