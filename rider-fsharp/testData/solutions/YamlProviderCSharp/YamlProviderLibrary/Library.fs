module YamlProviderLibrary

open FSharp.Configuration

type GeneratedConfig = YamlConfig<YamlText = """ 
Level1:
  Level12:
    Level13:
      -
        name: Alex
        age: 22
      -
        name: Eugene
        age: 25
Level2:
  Level21: 2""">

let configInstance = GeneratedConfig()
  
let funcWithNestedProvidedType (x: GeneratedConfig.Level1_Type) = () 
let funcWithNestedProvidedTypeArray (x: GeneratedConfig.Level1_Type[]) = () 








