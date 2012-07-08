namespace JSBuilder.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting   
open References
open StringUtils
open Assertions
open System.Collections.Generic
open System.IO
open System
open System.Linq
open PathUtils
open ApprovalTests

[<TestClass>]
type ``When manipulating paths``() =
    
    [<TestMethod>]        
    member this.``should be able to convert to relative paths``() =
        makePathRelativeTo 
            @"C:\Chocolatey\lib\gittfs.0.14.0\tools" 
            @"C:\Chocolatey\lib\"
        |> Equals @"gittfs.0.14.0/tools"


    [<TestMethod>]        
    member this.``should be able to convert to relative paths with forward slashes``() =
        makePathRelativeTo 
            @"C:/Chocolatey/lib/gittfs.0.14.0/tools" 
            @"C:/Chocolatey/lib/"
        |> Equals @"gittfs.0.14.0/tools"