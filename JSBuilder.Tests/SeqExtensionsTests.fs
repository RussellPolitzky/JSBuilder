namespace SeqExtensionsTests

open Microsoft.VisualStudio.TestTools.UnitTesting    
open Assertions


[<TestClass>]
type Test_SeqExtensions() =
   
    [<TestMethod>]        
    member this.``should be able to convert a sequence to a string``() =
        [1;2;3;4;5] 
        |> Seq.toSingleSring
        |> Equals "12345"

    [<TestMethod>]        
    member this.``should be able to convert a sequence to a string using custom transformer``() =
            [1;2;3;4;5] 
            |> Seq.toSingleSringUsingConverterAndSeparator (fun i -> i.ToString()) "+"
            |> Equals "1+2+3+4+5"

    [<TestMethod>]        
    member this.``should not find duplicates in collection``() =
        [1;2;3;4;5] 
        |> Seq.hasDuplicates
        |> Equals false

    [<TestMethod>]        
    member this.``should be find duplicates in collections``() =
        [1;2;3;4;5;1] 
        |> Seq.hasDuplicates
        |> Equals true
