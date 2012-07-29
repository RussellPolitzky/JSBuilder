module ExcelReader

open StringUtils
open System.IO
open System.Text.RegularExpressions
open OfficeOpenXml

/// Record modelling includes configuration
/// for a 
type IncludesConfig = {
    mutable BuildIncludes            :bool      // If this is false then ignore this instruction.
    mutable WebApplicationRootFolder :string    // Absolute path to root folder. 
    mutable RootScript               :string    // JavaScript application root - all references stem from this.
    mutable SourceTemplatePath       :string    // HTML template to use for the index page of the web  
    mutable TargetHTMLFile           :string    // Target for completed template
    mutable SourceFolders            :string[]  // Folders containing JavaScript source.
    mutable IgnoreReferenceIn        :string[]  // Disregard any references in these files.
};


/// Given range of cells from a row in an 
// excel sheet, this function transforms them
// into a configuration item.
let getIncludesConfigItem (cells:seq<ExcelRangeBase>) = 
    let col (cellref:string) = Regex.Match(cellref, @"([A-Z])").Groups.[1].Value
    let config = 
            {
                BuildIncludes            = false
                WebApplicationRootFolder = ""
                RootScript               = ""
                SourceTemplatePath       = ""
                TargetHTMLFile           = ""
                SourceFolders            = [||]
                IgnoreReferenceIn        = [||]
            }
    cells 
    |> Seq.iter (fun cell -> match (cell.Address |> col) with
                             | "A" -> config.BuildIncludes           <- cell.Value |> toString |> toLowercase = "y"
                             | "B" -> config.WebApplicationRootFolder<- cell.Value |> toString
                             | "C" -> config.RootScript              <- cell.Value |> toString
                             | "D" -> config.SourceTemplatePath      <- cell.Value |> toString
                             | "E" -> config.TargetHTMLFile          <- cell.Value |> toString
                             | "F" -> config.SourceFolders           <- cell.Value |> toString |> split [|';'|]
                             | "G" -> config.SourceFolders           <- cell.Value |> toString |> split [|';'|]
                             | _ -> ()) // ignore this data since its outside 
    config // return 

/// Reads rows from the given Excel sheet and tab and then 
/// passes them to a mappping function for conversion into a 
/// record, which in this case is a configuration item. 
/// This version of the function is a higher order one,
/// taking the transformer function as an argument.
let inline private _readConfigFrom transformToConfigItem fileName (tabName:string) =
    let row (cellref:string) = Regex.Match(cellref, @"(\d+)"  ).Groups.[1].Value
    let skipHeaderLine = Seq.skip 1 

    use package = new ExcelPackage(new FileInfo(fileName))
    package.Workbook.Worksheets.[tabName].Cells
    |> Seq.groupBy (fun cell -> cell.Address |> row)
    |> skipHeaderLine
    |> Seq.map(fun gp -> snd gp |> transformToConfigItem)

/// Reads rows from the given Excel sheet and tab 
/// and maps them to IncludesConfig items.
let readIncludesConfig fileName tabName = 
    _readConfigFrom getIncludesConfigItem fileName tabName // notice injection here.

    
                    
    
