
#r "nuget: FSharp.Text.RegexProvider, 2.1.0"

open FSharp.Text.RegexProvider

type PhoneRegexWithPrefix = Regex< @"(?<AreaCode>^\d{3})-(?<PhoneNumber>\d{3}-\d{4}$)" >

let res: string = PhoneRegexWithPrefix().TypedMatch("425-123-2345").AreaCode.Value
