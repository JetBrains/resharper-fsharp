namespace TypeProviderLibrary

open FSharp.Configuration

type GeneratedConfig = YamlConfig<YamlText = """
Level1:
  Level12:
    Level13: 1
Level2:
  Level21: 2""">


module Config = let instance = GeneratedConfig()
