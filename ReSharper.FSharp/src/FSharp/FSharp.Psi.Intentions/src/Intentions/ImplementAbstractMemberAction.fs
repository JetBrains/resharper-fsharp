namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Intentions.Implementor
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<ContextAction(Name = "ImplementAbstractMember", GroupType = typeof<FSharpContextActions>,
                Description = "Implements abstract member in derived types", Priority = -20s)>]
type ImplementAbstractMemberAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ImplementMemberBase()

    let getMemberDeclaration () =
        dataProvider.GetSelectedElement<IAbstractMemberDeclaration>()

    override this.Text = "Implement in derived types"

    override this.IsAvailable _ =
        let memberDecl = getMemberDeclaration ()
        if isNull memberDecl || not (isAtTreeNode dataProvider memberDecl.NameIdentifier) then false else

        let overridableMember = memberDecl.DeclaredElement.As<IOverridableMember>()
        isNotNull overridableMember && overridableMember.IsAbstract

    override this.GetMemberToImplement() =
        getMemberDeclaration().DeclaredElement :?> IOverridableMember

    override this.GetTargetTypes(progress) =
        let overridableMember = this.GetMemberToImplement()
        OverridableMembersFinder.FindOverridableNotHiddenMembersInInheritors(
            overridableMember.ContainingType, overridableMember, progress)

    override this.SelectPartText = "Implement member in"
    override this.GeneratorKind = GeneratorStandardKinds.Overrides

    interface IContextAction with
        member this.IsAvailable(cache) = this.IsAvailable(cache)
        member this.CreateBulbItems() = this.ToContextActionIntentions()
