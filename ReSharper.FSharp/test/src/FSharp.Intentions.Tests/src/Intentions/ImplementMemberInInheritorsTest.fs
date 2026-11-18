namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.Application.Progress
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Intentions.Implementor
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type ImplementMemberInInheritorsTest() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "features/intentions/implementMemberInInheritors"

    member x.DoNamedTestFsCs() =
        x.DoTestSolution(FSharpTestUtil.referenceCSharpProject x)

    [<Test>] member x.``Class - Method 01``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Class - Property 01``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Class - Property 02 - Getter implemented``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Interface - Method 01``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Interface - Method 02 - Partial impl``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Interface - Property 01``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Interface - Property 02 - Setter``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Interface - Property 03 - Getter implemented``() = x.DoNamedTestFsCs()


    override this.DoTest(lifetime: Lifetime, testProject: IProject) =
        let textControl = this.OpenTextControl(lifetime)
        let memberName = BaseTestWithTextControl.GetSetting(textControl, "MEMBER")
        Assert.IsNotNull(memberName, "Setting ${MEMBER:...} is missing in the test file")

        let typeDecl = TextControlToPsi.GetElement<ITypeDeclaration>(this.Solution, textControl)
        let typeElement = typeDecl.DeclaredElement

        let memberToImplement =
            typeElement.GetAllSuperTypeElements()
            |> Seq.collect _.GetMembers()
            |> Seq.pick (function
                | :? IOverridableMember as overridableMember when overridableMember.ShortName = memberName ->
                    Some overridableMember
                | _ -> None)

        let targetTypes =
            OverridableMembersFinder.FindOverridableNotHiddenMembersInInheritors(
                memberToImplement.ContainingType, memberToImplement, NullProgressIndicator.Create())

        Assert.IsNotEmpty(targetTypes, "No target types")

        MemberImplementorPreparer.ImplementMember(
            memberToImplement, targetTypes, "Implement member in", GeneratorStandardKinds.Overrides, textControl,
            (fun _ -> ()), (fun _ -> ()))

        this.CheckProject(testProject)
