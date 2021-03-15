module TypeProviderLibrary.RegexProvider

open FSharp.Text.RegexProvider
open FSharp.Text.RegexExtensions

type PhoneRegexWithPrefix = Regex< @"(?<AreaCode>^\d{3})-(?<PhoneNumber>\d{3}-\d{4}$)" >
type PhoneRegex = Regex< @"(?<AreaCode>^\d{3})-(?<PhoneNumber>\d{3}-\d{4}$)", noMethodPrefix = true >
type NumberRegex = Regex< @"(?<Number>\d+)">


let res1: string = PhoneRegexWithPrefix().TypedMatch("425-123-2345").AreaCode.Value
let res2: string = PhoneRegex().Match("425-123-2345").AreaCode.Value
let res3: string = NumberRegex().TypedReplace("", fun m -> m.Number.AsInt.ToString())
