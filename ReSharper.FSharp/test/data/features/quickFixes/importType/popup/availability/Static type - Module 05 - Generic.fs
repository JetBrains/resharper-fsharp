module Test

module A = 
    module ModuleOrType =
        ()

module B =
    type ModuleOrType<'a> = class end


nameof(ModuleOrType{caret}<int>)
