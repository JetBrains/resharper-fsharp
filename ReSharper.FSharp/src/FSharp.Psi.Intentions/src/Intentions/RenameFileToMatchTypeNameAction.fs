namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ContextActions

open System
open System.Collections.Generic
open JetBrains.DocumentManagers.Transactions
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Intentions.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<ContextAction(Group = "F#", Name = "Match file name with type name", Priority = 1s,
                Description = "Renames current file to match the name of the single type or a top-level module.")>]
type RenameFileToMatchTypeNameAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let isApplicable (typeElement: ITypeElement) =
        let types = HashSet()
        let modules = HashSet<IFSharpModule>()

        let decls = dataProvider.PsiFile.ModuleDeclarations
        if decls.Count = 1 && decls.[0] :? INamedModuleDeclaration then
            decls.[0].DeclaredElement.Equals(typeElement) else

        for moduleDecl in decls do
            for md in moduleDecl.MembersEnumerable do
                match md with
                | :? IModuleDeclaration as moduleDecl ->
                    modules.Add(moduleDecl.DeclaredElement :?> _) |> ignore

                | :? ITypeDeclarationGroup as typeDeclGroup ->
                    for t in typeDeclGroup.TypeDeclarations do
                        types.Add(t.DeclaredElement) |> ignore

                | :? IExceptionDeclaration as e ->
                    types.Add(e.DeclaredElement) |> ignore

                | _ -> ()

        match types.SingleItem(), modules.SingleItem() with
        | null, null -> false
        | null, m -> types.IsEmpty() && m.Equals(typeElement)
        | t, null -> modules.IsEmpty() && t.Equals(typeElement)
        | t, m -> t.Equals(m.AssociatedTypeElement) && (m.Equals(typeElement) || t.Equals(typeElement))

    override this.Text =
        match dataProvider.GetSelectedElement<IFSharpTypeElementDeclaration>() with
        | :? IModuleDeclaration -> "Rename file to match module name"
        | _ -> "Rename file to match type name"

    override this.IsAvailable _ =
        let declaration = dataProvider.GetSelectedElement<IFSharpTypeElementDeclaration>()
        if not (isValid declaration) then false else

        let range = dataProvider.SelectedTreeRange
        if not (declaration.GetNameRange().Contains(&range)) then false else

        let typeElement = declaration.DeclaredElement
        if isNull typeElement || not (isApplicable typeElement) then false else

        let sourceFile = dataProvider.SourceFile
        let projectFile = RenameFileToMatchTypeNameActionBase.TryGetProjectFileToRename(typeElement, sourceFile)
        if isNull projectFile then false else

        RenameFileToMatchTypeNameActionBase.TypeNameNameDoesNotCorrespondWithFileName(typeElement, projectFile)

    override this.ExecutePsiTransaction(solution, _) =
        let typeDeclaration = dataProvider.GetSelectedElement<IFSharpTypeElementDeclaration>()
        let typeElement = typeDeclaration.DeclaredElement
        let sourceFile = dataProvider.SourceFile
        let projectFile = RenameFileToMatchTypeNameActionBase.TryGetProjectFileToRename(typeElement, sourceFile)

        let newName = RenameFileToMatchTypeNameActionBase.GetFileName(typeElement, projectFile)
        if isNull newName then null else

        Action<_>(fun _ ->
            let newPath = projectFile.Location.Directory.Combine(newName)
            if newPath.ExistsFile then
                MessageBox.ShowError(
                    sprintf "File '%s' already exists" newName, sprintf "Can't rename '%s'" projectFile.Location.Name)
            else
                use cookie = solution.CreateTransactionCookie(DefaultAction.Commit, this.Text)
                cookie.Rename(projectFile, newName))
