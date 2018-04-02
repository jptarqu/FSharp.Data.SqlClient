// Learn more about F# at http://fsharp.org

open System

open System.Data.SqlClient;
open FSharp.Data.SqlClient.Extensions
open AdoCoreGenerator

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    let connsql = @"Data Source=csbfl-web1-test.csbfl.local\sqlexpress; Initial Catalog=XpressLoanRequests;Integrated Security=True";
    use conn = new SqlConnection()
    conn.ConnectionString <- connsql;
    conn.Open()
    conn.LoadDataTypesMap()
    let sqlData = FSharp.Data.Generator(conn, "dbo" )
    
    let helper = TemplateHelper()
    
    
    let template = "@foreach(var n in Model.RoutineNames) {
                         @n
                    }"
    helper.LoadTemplates( [( "sp", template)] )
    printfn "%s" (helper.Generate("sp", sqlData))
    0 // return an integer exit code
