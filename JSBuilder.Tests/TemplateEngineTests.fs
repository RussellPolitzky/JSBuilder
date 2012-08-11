module TemplateEngineTests

open System
open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting    
open Assertions
open ExcelReader
open TemplateEngine


[<TestClass>]
type TemplateEngineTests() =

    [<TestMethod>]        
    member this.``effect replacements in template``() =
        let template = @"
            <!DOCTYPE html>
            <html xmlns=""http://www.w3.org/1999/xhtml"">
            <head>
                {jsinclude}
                <title></title>
            </head>
            <body>
                {bodyContent}
            </body>
            </html>
            "
        let mockIncludes = @"<script type=""javascript"" src=""this.js"">"
        let bodyContent = "Some text to put into the body."
        let replacements = [ "jsinclude", mockIncludes; 
                             "bodyContent", bodyContent  ] 
                           |> Map.ofList
        buildTemplateInstance replacements template
        |> IsSameStringAs @"
            <!DOCTYPE html>
            <html xmlns=""http://www.w3.org/1999/xhtml"">
            <head>
                <script type=""javascript"" src=""this.js"">
                <title></title>
            </head>
            <body>
                Some text to put into the body.
            </body>
            </html>
            "
        


        
