namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

//TODO: toSetting ?
type AttributePlacementOptions() = class end

[<ContextAction(Group = "F#", Name = "Add attribute to type", Priority = 1s,
                Description = "Adds an attribute to type")>]
type AddAttributeToTypeAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(var0) = failwith "todo"
    override this.IsAvailable(cache) = failwith "todo"
    override this.Text = "Add attribute"

[<ContextAction(Group = "F#", Name = "Add attribute to member", Priority = 1s,
                Description = "Adds an attribute to member")>]
type AddAttributeToMemberAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(var0) = failwith "todo"
    override this.IsAvailable(cache) = failwith "todo"
    override this.Text = "Add attribute"

[<ContextAction(Group = "F#", Name = "Add attribute to module", Priority = 1s,
                Description = "Adds an attribute to member")>]
type AddAttributeToModuleAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(var0) = failwith "todo"
    override this.IsAvailable(cache) = failwith "todo"
    override this.Text = "Add attribute"

[<ContextAction(Group = "F#", Name = "Add attribute to module", Priority = 1s,
                Description = "Adds an attribute to member")>]
type AddAttributeToParameterAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    override this.ExecutePsiTransaction(var0) = failwith "todo"
    override this.IsAvailable(cache) =
        match dataProvider.GetSelectedElement<IParametersPatternDeclaration>() with
        | null ->
            false
        | parameter ->

            // check if attribute already exist
            // check if need to add parens
            // add attribute
            // move caret

            true

    override this.Text = "Add attribute"

// TODO: move this to tests
// module ParametersTest =
//
//      let f a{caret} = ()
//      let f ([<{caret}>] a) = ()
//
//      let f1 ([<Optional>] a{caret}) = ()
//      let f1 ([<Optional; {caret}>] a) = ()
//
//      type A() =
//         member _.F a{caret} = ()
//         member _.F ([<{caret}>] a) = ()
//
//         member _.F1 ([<Optional>] a{caret}) = ()
//         member _.F2 ([<Optional; {caret}>] a) = ()