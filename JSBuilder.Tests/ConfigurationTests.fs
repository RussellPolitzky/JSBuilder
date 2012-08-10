namespace ConfigurationTests

open System
open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting    
open Assertions
open ExcelReader

[<TestClass>]
type ConfigurationTests() =

    let testFile = @"JSBuilderConfig.xlsx"
    let testTab  = @"Includes"

    [<TestMethod>]        
    member this.``read configuration for js includes``() =
        let a = readIncludesConfig testFile testTab 
                |> Seq.map (fun i -> sprintf "%A" i)
                |> toSingleSringWithSep Environment.NewLine
        a |> IsSameStringAs @"{BuildIncludes = true;
 WebApplicationRootFolder = ""WebProject"";
 RootScript = ""Tests\alltests.js"";
 SourceTemplatePath = ""WebProject\Templates"";
 TargetHTMLFile = ""Tests.html"";
 SourceFolders = [|""jquery.signalR.js""|];
 IgnoreReferenceIn = [||];}
{BuildIncludes = true;
 WebApplicationRootFolder = ""WebProject"";
 RootScript = ""Scripts\app.js"";
 SourceTemplatePath = ""WebProject\Templates"";
 TargetHTMLFile = ""App.html"";
 SourceFolders = [|""jquery.signalR.js""|];
 IgnoreReferenceIn = [||];}"
        