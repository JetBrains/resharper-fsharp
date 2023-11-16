Public Interface I
    Property P As String
End Interface

Public Class MyType
    Implements I

    Public Property P1 As String Implements I.P
    Public Property P2 As String
    Protected Property P3 As String
End Class
