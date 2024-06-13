using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using JetBrains.Util.DataStructures;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public abstract class FSharpTypePart<T> : TypePartImplBase<T>, IFSharpTypePart
    where T : class, IFSharpTypeElementDeclaration
  {
    public ExtensionMemberInfo[] CSharpExtensionMemberInfos { get; } = EmptyArray<ExtensionMemberInfo>.Instance;
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

      var methods = new LocalList<ExtensionMemberInfo>();
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
        
        methods.Add(new ExtensionMemberInfo(AnyCandidateType.INSTANCE, offset, member.DeclaredName, ExtensionMemberKind.ExtensionMethod, this));
      }

      if (methods.IsEmpty())
        return;

      CSharpExtensionMemberInfos = methods.ToArray();
    }

    public override ExtensionMemberInfo[] ExtensionMemberInfos =>
      CSharpExtensionMemberInfos;

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

    public override HybridCollection<ITypeMember> FindExtensionMethod(ExtensionMemberInfo info)
    {
      var typeElement = TypeElement;
      if (typeElement == null)
        return HybridCollection<ITypeMember>.Empty;

      var declaration = GetDeclaration();
      if (declaration == null)
        return HybridCollection<ITypeMember>.Empty;

      var languageLevel = declaration.GetFSharpLanguageLevel();
      if (languageLevel < FSharpLanguageLevel.FSharp80 && 
          !typeElement.HasAttributeInstance(PredefinedType.EXTENSION_ATTRIBUTE_CLASS, false))
        return HybridCollection<ITypeMember>.Empty;

      var result = HybridCollection<ITypeMember>.Empty;
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
      Modifiers = MemberDecoration.FromRawValue(reader.ReadUShort());
      AttributeClassNames = reader.ReadStringArray();
      var extensionMethodCount = reader.ReadOftenSmallPositiveInt();
      if (extensionMethodCount == 0)
        return;

      var methods = new ExtensionMemberInfo[extensionMethodCount];
      for (var i = 0; i < extensionMethodCount; i++)
        methods[i] = new ExtensionMemberInfo(reader, ExtensionMemberKind.ExtensionMethod, this);
      CSharpExtensionMemberInfos = methods;
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteString(SourceName);
      writer.WriteUShort(Modifiers.ToRawValue());
      writer.WriteStringArray(AttributeClassNames);

      writer.WriteOftenSmallPositiveInt(CSharpExtensionMemberInfos.Length);
      foreach (var info in CSharpExtensionMemberInfos)
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
      if (AttributeClassNames.IsEmpty())
        return EmptyList<IAttributeInstance>.Instance;

      if (!(GetDeclaration()?.GetFcsSymbol() is FSharpEntity entity))
        return EmptyList<IAttributeInstance>.Instance;

      var psiModule = GetPsiModule();
      var entityAttrs = entity.Attributes;

      if (entityAttrs.Count == 0)
        return EmptyList<IAttributeInstance>.Instance;

      var result = new FrugalLocalList<IAttributeInstance>();
      foreach (var fcsAttribute in entityAttrs)
        if (new ClrTypeName(fcsAttribute.AttributeType.BasicQualifiedName).Equals(clrName))
          result.Add(new FSharpAttributeInstance(fcsAttribute, psiModule));

      return result.ResultingList();
    }

    public override bool HasAttributeInstance(IClrTypeName clrTypeName)
    {
      if (!MayHaveAttribute(clrTypeName))
        return false;

      // todo: get entity without getting declaration
      var entity = GetDeclaration()?.GetFcsSymbol() as FSharpEntity;
      return entity?.Attributes.HasAttributeInstance(clrTypeName.FullName) ?? false;
    }

    public override IList<IAttributeInstance> GetTypeParameterAttributeInstances(int index, IClrTypeName typeName) =>
      EmptyList<IAttributeInstance>.Instance; // todo

    public override bool HasTypeParameterAttributeInstance(int index, IClrTypeName typeName) =>
      false; // todo

    // todo: attribute type abbreviation
    private bool MayHaveAttribute(IClrTypeName clrTypeName) =>
      AttributeClassNames.Contains(clrTypeName.ShortName) || 
      AttributeClassNames.Contains(clrTypeName.ShortName.SubstringBeforeLast("Attribute"));

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
