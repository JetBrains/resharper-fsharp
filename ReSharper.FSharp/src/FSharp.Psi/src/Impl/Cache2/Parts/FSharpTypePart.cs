using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public abstract class FSharpTypePart<T> : TypePartImplBase<T>, IFSharpTypePart 
    where T : class, IFSharpTypeElementDeclaration
  {
    public override ExtensionMethodInfo[] ExtensionMethodInfos { get; } = EmptyArray<ExtensionMethodInfo>.Instance;

    protected FSharpTypePart([NotNull] T declaration, [NotNull] string shortName, MemberDecoration memberDecoration,
      int typeParameters, [NotNull] ICacheBuilder cacheBuilder) : base(declaration, shortName, typeParameters)
    {
      Modifiers = memberDecoration;

      var attributes = declaration.GetAttributes();
      var attrNames = new FrugalLocalHashSet<string>();

      foreach (var attr in attributes)
        attrNames.Add(cacheBuilder.Intern(attr.LongIdentifier?.Name.GetAttributeShortName()));
      AttributeClassNames = attrNames.ToArray();

      if (!attributes.Any(a => a.ShortNameEquals("Extension")))
        return;

      var methods = new LocalList<ExtensionMethodInfo>();
      foreach (var member in declaration.MemberDeclarations)
      {
        if (!member.GetAttributes().Any(a => a.ShortNameEquals("Extension")))
          continue;

        var offset = member.GetTreeStartOffset().Offset;
        methods.Add(new ExtensionMethodInfo(AnyCandidateType.INSTANCE, offset, member.DeclaredName) {Owner = this});
      }
      ExtensionMethodInfos = methods.ToArray();
    }

    public override HybridCollection<IMethod> FindExtensionMethod(ExtensionMethodInfo info)
    {
      var declaration = GetDeclaration();
      if (declaration == null)
        return HybridCollection<IMethod>.Empty;

      var result = HybridCollection<IMethod>.Empty;
      foreach (var memberDeclaration in declaration.MemberDeclarations)
        if (info.ShortName == memberDeclaration.DeclaredName &&
            info.Hash == memberDeclaration.GetTreeStartOffset().Offset)
        {
          if (memberDeclaration.DeclaredElement is IMethod method)
            result = result.Add(method);
        }
      return result;
    }

    protected FSharpTypePart(IReader reader) : base(reader)
    {
      Modifiers = MemberDecoration.FromInt(reader.ReadInt());
      AttributeClassNames = reader.ReadStringArray();
      var extensionMethodCount = reader.ReadInt();
      if (extensionMethodCount <= 0)
        return;

      var methods = new ExtensionMethodInfo[extensionMethodCount];
      for (var i = 0; i < extensionMethodCount; i++)
        methods[i] = new ExtensionMethodInfo(reader) {Owner = this};
      ExtensionMethodInfos = methods;
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteInt(Modifiers.ToInt());
      writer.WriteStringArray(AttributeClassNames);

      writer.WriteInt(ExtensionMethodInfos.Length);
      foreach (var info in ExtensionMethodInfos)
        info.Write(writer);
    }

    protected override ICachedDeclaration2 FindDeclaration(IFile file, ICachedDeclaration2 candidateDeclaration)
    {
      if (Offset < TreeOffset.Zero) return null;
      if (candidateDeclaration is T) return candidateDeclaration;
      return null;
    }

    public override string[] ExtendsListShortNames => EmptyArray<string>.Instance;
    public override MemberDecoration Modifiers { get; }
    public override bool CanBePartial => true; // workaround for F# signatures

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName)
    {
      if (!((GetDeclaration() as IFSharpTypeDeclaration)?.GetFSharpSymbol() is FSharpEntity entity))
        return EmptyList<IAttributeInstance>.Instance;

      var attrs = new List<IAttributeInstance>();
      foreach (var attr in entity.Attributes)
        attrs.Add(new FSharpAttributeInstance(attr, GetPsiModule()));
      return attrs;
    }

    public override bool HasAttributeInstance(IClrTypeName clrTypeName)
    {
      // todo: get entity without getting declaration 
      var entity = (GetDeclaration() as IFSharpTypeDeclaration)?.GetFSharpSymbol() as FSharpEntity;
      return entity?.Attributes.HasAttributeInstance(clrTypeName.FullName) ?? false;
    }

    public override IList<IAttributeInstance> GetTypeParameterAttributeInstances(int index, IClrTypeName typeName)
    {
      // todo
      return EmptyList<IAttributeInstance>.Instance;
    }

    public override bool HasTypeParameterAttributeInstance(int index, IClrTypeName typeName)
    {
      // todo
      return false;
    }

    public override string[] AttributeClassNames { get; }

    public override string ToString()
    {
      var typeElement = TypeElement?.ToString() ?? "null";
      var typeParameters = PrintTypeParameters();

      return $"{GetType().Name}:{ShortName}{typeParameters}->{typeElement}";
    }

    protected virtual string PrintTypeParameters() => "";

    public string SourceName =>
      GetDeclaration()?.SourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;
  }
}
