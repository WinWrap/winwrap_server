'#Language "WWB.NET"

Imports System
Imports System.Collections.Generic

Class Person
    Property Name As String
    Property Age As Integer

    Public Sub New(NameV As String, AgeV As Integer)
        Name = NameV
        Age = AgeV
    End Sub
End Class

Sub Main()
    Dim map As New Dictionary(Of String, Person)
    map("Tom") = New Person("Tom", 21)
    map("Dick") = New Person("Dick", 25)
    map("Harry") = New Person("Harry", 19)
    For Each kvp As KeyValuePair(Of String, Person) In map
        Debug.Print($"{kvp.Key} is {kvp.Value.Age} years old")
    Next
    Stop
End Sub
