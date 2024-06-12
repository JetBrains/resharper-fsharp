using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class ModulePartBase<T> : FSharpTypePart<T>, IModulePart
    where T : class, IModuleDeclaration
  {
    private ExtensionMemberInfo[] FSharpExtensionMemberInfos { get; } = EmptyArray<ExtensionMemberInfo>.Instance;
    
    protected ModulePartBase([NotNull] T declaration, [NotNull] string shortName, MemberDecoration memberDecoration,
      [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, shortName, memberDecoration, 0, cacheBuilder)
    {
      var extensionMemberInfos = new LocalList<ExtensionMemberInfo>();

      foreach (var fsDecl in EnumerateExtensionMembers(declaration))
      {
        // todo: use candidate type
        var offset = fsDecl.GetTreeStartOffset().Offset;
        var sourceName = fsDecl.SourceName;
        var extensionMemberInfo = new ExtensionMemberInfo(AnyCandidateType.INSTANCE, offset, sourceName, FSharpExtensionMemberKind.FSharpExtensionMember, this);
        extensionMemberInfos.Add(extensionMemberInfo);
      }

      FSharpExtensionMemberInfos = extensionMemberInfos.ToArray();
    }

    private static IEnumerable<IFSharpDeclaration> EnumerateExtensionMembers(T declaration)
    {
      foreach (var moduleMember in declaration.MembersEnumerable)
      {
        if (moduleMember is not ITypeDeclarationGroup typeDeclGroup)
          continue;

        foreach (var typeDecl in typeDeclGroup.TypeDeclarations)
        {
          if (typeDecl is not ITypeExtensionDeclaration { IsTypePartDeclaration: false } typeExtensionDecl)
            continue;

          foreach (var memberDecl in typeExtensionDecl.MemberDeclarations)
          {
            if (memberDecl is not IFSharpDeclaration fsDecl)
              continue;

            yield return fsDecl;
          }
        }
      }
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);

      writer.WriteOftenSmallPositiveInt(FSharpExtensionMemberInfos.Length);
      foreach (var info in FSharpExtensionMemberInfos)
        info.Write(writer);
    }

    protected ModulePartBase(IReader reader) : base(reader)
    {
      var extensionMemberCount = reader.ReadOftenSmallPositiveInt();
      if (extensionMemberCount == 0)
        return;

      var methods = new ExtensionMemberInfo[extensionMemberCount];
      for (var i = 0; i < extensionMemberCount; i++)
        methods[i] = new ExtensionMemberInfo(reader, FSharpExtensionMemberKind.FSharpExtensionMember, this);
      FSharpExtensionMemberInfos = methods;
    }

    public override HybridCollection<ITypeMember> FindExtensionMethod(ExtensionMemberInfo info)
    {
      if (FSharpExtensionMemberInfos.Length > 0 && GetDeclaration() is { } decl)
        foreach (var fsDecl in EnumerateExtensionMembers(decl))
          if (fsDecl.GetTreeStartOffset().Offset == info.Hash && fsDecl.DeclaredElement is ITypeMember typeMember)
            return new HybridCollection<ITypeMember>(typeMember);

      return base.FindExtensionMethod(info);
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpModule(this);

    public IEnumerable<ITypeMember> GetTypeMembers() =>
      GetDeclaration() is { } declaration
        ? declaration.MemberDeclarations.Select(d => d.DeclaredElement).WhereNotNull()
        : EmptyList<ITypeMember>.Instance;

    public IEnumerable<IDeclaredType> GetSuperTypes() => new[] {GetBaseClassType()};
    public IDeclaredType GetBaseClassType() => GetPsiModule().GetPredefinedType().Object;

    public MemberPresenceFlag GetMemberPresenceFlag() => MemberPresenceFlag.NONE;

    public override ExtensionMemberInfo[] ExtensionMemberInfos =>
      ArrayModule.Append(CSharpExtensionMemberInfos, FSharpExtensionMemberInfos);

    public override MemberDecoration Modifiers
    {
      get
      {
        var modifiers = base.Modifiers;
        modifiers.IsAbstract = true;
        modifiers.IsSealed = true;
        modifiers.IsStatic = true;

        return modifiers;
      }
    }

    public override IDeclaration GetTypeParameterDeclaration(int index) => throw new InvalidOperationException();
    public override string GetTypeParameterName(int index) => throw new InvalidOperationException();
    public override TypeParameterVariance GetTypeParameterVariance(int index) => throw new InvalidOperationException();
    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index) => throw new InvalidOperationException();
    public override bool IsNullableContextEnabledForTypeParameter(int index) => throw new InvalidOperationException();


    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index) =>
      throw new InvalidOperationException();

    public virtual bool IsAnonymous => false;
    public abstract ModuleMembersAccessKind AccessKind { get; }

    public ITypeElement AssociatedTypeElement =>
      GetDeclaration() is INestedModuleDeclaration moduleDeclaration
        ? ((ITypeDeclaration) moduleDeclaration.GetAssociatedTypeDeclaration(out _))?.DeclaredElement
        : null;
  }
}
