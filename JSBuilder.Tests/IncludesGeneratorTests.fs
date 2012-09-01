module IncludesGeneratorTests

open System
open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting    
open Assertions
open ExcelReader
open IncludesGenerator


/// Mock function - added to class to keep state.
type MockIncludesBuilder() = 
       let mutable wasCalled = false
       let mutable _absolutePathToAppDirectory = ""
       member this.mockIncludesBuilder pathToRootScript absolutePathToAppDirectory (filesToIgnore:string[]) = 
              _absolutePathToAppDirectory <- absolutePathToAppDirectory 
              wasCalled <- true 
              "Mock includes"
       member x.WasCalled = wasCalled
       member x.AbsolutePathToAppDirectory = _absolutePathToAppDirectory


/// Mock function - added to class to keep state.
type MockTemplatePopulator() = 
       let mutable wasCalled = false
       let mutable _fullPathToTemplate = ""
       member this.mockTemplatePopulator fullPathToTemplate fullOutputPath includesSection = 
              _fullPathToTemplate <-fullPathToTemplate;  wasCalled <- true; ()
       member x.WasCalled = wasCalled
       member x.FullPathToTemplate = _fullPathToTemplate


[<TestClass>]
type IncludesGeneratorTests() =


// This test method is somewhat interesting in that it 
    // shows how to create mock functions and inject them 
    // into higher order functions.  In this case, we use 
    // functions attached to objects so that we can easily
    // create side effects that we can test afterwards.
    [<TestMethod>]        
    member this.``should not call if condig is ignored``() =
    
        let mockIncludesBuilder = new MockIncludesBuilder()
        let mockTemplatePopulator = new MockTemplatePopulator()
            
        // Arrange
        let testConfig = 
                {
                    BuildIncludes            = false // NB don't do anything with this one .. 
                    WebApplicationRootFolder = "TestWebApplication"
                    RootScript               = ""
                    SourceTemplatePath       = @"Templates\Tests.template"
                    TargetHTMLFile           = ""
                    IgnoreReferenceIn        = [||]
                }

        // Act
        _processConfig 
            mockIncludesBuilder.mockIncludesBuilder 
            mockTemplatePopulator.mockTemplatePopulator
            testConfig

        // Assert
        mockIncludesBuilder.WasCalled |> IsFalse
        mockTemplatePopulator.WasCalled |> IsFalse
        

    // This test method is somewhat interesting in that it 
    // shows how to create mock functions and inject them 
    // into higher order functions.  In this case, we use 
    // functions attached to objects so that we can easily
    // create side effects that we can test afterwards.
    [<TestMethod>]        
    member this.``should find the web application abs folder``() =
    
        let mockIncludesBuilder = new MockIncludesBuilder()
        let mockTemplatePopulator = new MockTemplatePopulator()
            
        // Arrange
        let testConfig = 
                {
                    BuildIncludes            = true 
                    WebApplicationRootFolder = "TestWebApplication"
                    RootScript               = ""
                    SourceTemplatePath       = @"Templates\Tests.template"
                    TargetHTMLFile           = ""
                    IgnoreReferenceIn        = [||]
                }

        // Act
        _processConfig 
            mockIncludesBuilder.mockIncludesBuilder 
            mockTemplatePopulator.mockTemplatePopulator
            testConfig

        // Assert
        mockIncludesBuilder.WasCalled |> IsTrue
        mockTemplatePopulator.WasCalled |> IsTrue
        mockIncludesBuilder.AbsolutePathToAppDirectory
          |> IsSameStringAs @"C:\srce\TestProjects\FSharpJSBuilder\TestWebApplication\"
        mockTemplatePopulator.FullPathToTemplate 
          |> IsSameStringAs @"C:\srce\TestProjects\FSharpJSBuilder\TestWebApplication\Templates\Tests.template"


 
