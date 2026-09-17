module Test

module A = 
    module MyModuleOrType =
        ()

module B = 
    type MyModuleOrType = class end

MyModuleOrType{caret}.M()
