module IncludesGenerator

open References
open ExcelReader
open PathUtils 
open System.Reflection
open System.IO


// we assume that the configuration
// spreadsheet is called JSBuilderConfig.xlsx
// and that it has a tab called Includes.

// Make these parameters.
//let configFileName = @"JSBuilderConfig.xlsx"
//let includesTabName = @"Includes"


let populateTemplate 
    (fullPathToTemplate:string) 
    (includesSection:string) = 
    // todo: Load up the template and replace the 
    // {includes} with the calculated deps.
    ()

/// Processes config to produce a populated
/// template.  This function requires that 
/// includes builder as well as the template
/// populator functions be injected and is
/// not intended to be used directly.
let _processConfig 
    (buildIncludesFor:string->string->string) 
    (templatePopulator:string->string->unit)
    (config:IncludesConfig) = 
    if (config.BuildIncludes) then // only process this is asked to do so.
        let absWebApplicationRoot = 
            findParentHavingRoot 
                (getCodeBasePathOfAssembly (Assembly.GetExecutingAssembly())) // should find the web dir from the currently executing assembly
                config.WebApplicationRootFolder
        let fullTemplatePath = combinePaths absWebApplicationRoot config.SourceTemplatePath
        buildIncludesFor config.RootScript absWebApplicationRoot
        |> templatePopulator fullTemplatePath

/// Processes config to produce a populated
/// template.  Takes a single IncludesConfig
/// parameter.
let processConfig = 
    _processConfig 
        buildIncludesSectionFor 
        populateTemplate

/// Entry point to this build task.
/// Produces HTML pages with populated 
/// includes as per the given configuration.
let processConfigurationsIn configFileName includesTabName = 
    readIncludesConfig configFileName includesTabName 
    |> Seq.iter (fun config -> processConfig config)




