namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.TypeProviders

open JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
open NUnit.Framework
open System

module PrimitiveTypesBoxerTest =

    type ColorEnum = Red = 0 | Blue = 1  
    
    let testData = [
        TestCaseData(SByte.MaxValue).SetName("SByte").Returns(SByte.MaxValue)
        TestCaseData(Int16.MaxValue).SetName("Short").Returns(Int16.MaxValue)
        TestCaseData(1).SetName("Int").Returns(1)
        TestCaseData(1L).SetName("Long").Returns(1L)
        TestCaseData(0xFF).SetName("Byte").Returns(0xFF)
        TestCaseData(UInt16.MaxValue).SetName("UShort").Returns(UInt16.MaxValue)
        TestCaseData(UInt32.MaxValue).SetName("UInt").Returns(UInt32.MaxValue)
        TestCaseData(UInt64.MaxValue).SetName("ULong").Returns(UInt64.MaxValue)
        TestCaseData(Decimal.MaxValue).SetName("Decimal").Returns(Decimal.MaxValue)
        TestCaseData(Single.Epsilon).SetName("Float").Returns(Single.Epsilon)
        TestCaseData(Double.Epsilon).SetName("Double").Returns(Double.Epsilon)
        TestCaseData('c').SetName("Char").Returns('c')
        TestCaseData(true).SetName("Bool").Returns(true)
        TestCaseData("string").SetName("String").Returns("string")
        TestCaseData(ColorEnum.Blue).SetName("Enum").Returns(1)
        TestCaseData(DBNull.Value).SetName("DBNull").Returns(DBNull.Value)
    ]

    [<TestCaseSource(nameof testData)>]
    let ``Server Boxing-Unboxing`` obj = PrimitiveTypesBoxer.BoxToServerStaticArg(obj).Unbox()
    
    [<TestCaseSource(nameof testData)>]
    let ``Client Boxing-Unboxing`` obj = PrimitiveTypesBoxer.BoxToClientStaticArg(obj).Unbox()
