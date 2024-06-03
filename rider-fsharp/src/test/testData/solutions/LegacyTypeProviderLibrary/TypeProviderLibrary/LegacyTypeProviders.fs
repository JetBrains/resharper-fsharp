module Library

open FSharp.Text.RegexProvider
open FSharp.Data

type PhoneRegex = Regex< @"(?<AreaCode>^\d{3})-(?<PhoneNumber>\d{3}-\d{4}$)" >
let _: string = PhoneRegex().TypedMatch("425-123-2345").AreaCode.Value


type Xml = XmlProvider<"Sample.lxml",
                       SampleIsList = true,
                       Global = true,
                       InferTypesFromValues = false>
let _: Xml.Excitation =
    Xml.Excitation(name = Some "",
                   ``type`` = "",
                   excite = Some "",
                   primitives = Some (Xml.Primitives(Xml.Box(xElement = null))))
