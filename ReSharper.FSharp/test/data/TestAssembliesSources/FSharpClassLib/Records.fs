namespace global

type RecordInGlobalNs =
    { Field: int }


module ModuleInGlobalNs =
    type Record =
        { Field: int }

    module NestedModule =
        type Record =
            { Field: int }

[<AutoOpen>]
module AutoOpenModuleInGlobalNs =
    type Record =
        { Field: int }


[<RequireQualifiedAccess>]
module RqaModuleInGlobalNs =
    type Record =
        { Field: int }


module ModuleWithModuleSuffix =
    type Record =
        { Field: int }

type ModuleWithModuleSuffix = int


namespace Ns

type RecordInNs =
    { Field: int }

module ModuleInNs =
    type Record =
        { Field: int }

    module NestedModule =
        type Record =
            { Field: int }
