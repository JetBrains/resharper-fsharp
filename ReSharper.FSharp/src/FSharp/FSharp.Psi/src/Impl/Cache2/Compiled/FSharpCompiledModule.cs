using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.Util;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
{
  public class FSharpCompiledModule : FSharpCompiledClassBase, IFSharpModule
  {
    public ModuleMembersAccessKind AccessKind { get; }
    private readonly ICompiledExtensionMemberProxy[] myExtensionMemberInfos;

    public override ICollection<ICompiledExtensionMemberProxy> ExtensionMembers =>
      ArrayModule.Append(base.ExtensionMembers.AsArray(), myExtensionMemberInfos);

    public FSharpCompiledModule(FSharpCompiledTypeRepresentation.Module repr,
      FSharpMetadataEntity entity, [NotNull] ICompiledEntity parent,
      [NotNull] IReflectionBuilder builder, [NotNull] IMetadataTypeInfo info) : base(entity, parent, builder, info)
    {
      AccessKind = GetModuleMembersAccessKind(info);

      var extensionMemberInfos = new LocalList<ICompiledExtensionMemberProxy>();
      foreach (var value in repr.values)
        if (value.IsExtensionMember)
          extensionMemberInfos.Add(new FSharpCompiledExtensionMemberInfo(value, this));

      myExtensionMemberInfos = extensionMemberInfos.ToArray();
    }

    public bool IsAnonymous =>
      Representation is FSharpCompiledTypeRepresentation.Module module && module.nameKind.IsAnon;

    public bool IsAutoOpen => AccessKind == ModuleMembersAccessKind.AutoOpen;
    public bool RequiresQualifiedAccess => AccessKind == ModuleMembersAccessKind.RequiresQualifiedAccess;

    public ITypeElement AssociatedTypeElement => null; // todo
    public string QualifiedSourceName => this.GetQualifiedName();

    private static ModuleMembersAccessKind GetModuleMembersAccessKind(IMetadataTypeInfo info)
    {
      if (info.HasCustomAttribute(FSharpPredefinedType.AutoOpenAttrTypeName.FullName))
        return ModuleMembersAccessKind.AutoOpen;

      if (info.HasCustomAttribute(FSharpPredefinedType.RequireQualifiedAccessAttrTypeName.FullName))
        return ModuleMembersAccessKind.RequiresQualifiedAccess;

      return ModuleMembersAccessKind.Normal;
    }
  }
}
