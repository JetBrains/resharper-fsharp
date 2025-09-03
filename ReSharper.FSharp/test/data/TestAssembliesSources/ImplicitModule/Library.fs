namespace global

type T() =
    class end

module T =
    let moduleValue = 1

    module NestedModuleType =
        let nestedModuleValue = 1

    type NestedModuleType() =
        class end

    type AnotherNestedModuleType() =
        class end
    
    module AnotherNestedModuleType =
        let nestedModuleValue = 1

type GenericT<'T>() =
    class end

module GenericT =
    let genericModuleValue = 1
