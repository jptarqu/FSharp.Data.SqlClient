namespace SqlClient

open FSharp.Data
open SqlClient.Extensions
open System.Data.SqlClient

module SpsModuleGenerator =
    open System.Data

    let private sampleAdo (con) =
        async {
            use cmd = new SqlCommand("dbo.MyStoredProcedure", con)
            cmd.CommandType <- CommandType.StoredProcedure
            return cmd.ExecuteNonQueryAsync() |> Async.AwaitTask
        }
        
    let private generateSpParamType (spName: string) (params : Parameter seq) = 
        let props = params |> Seq.map (fun p -> )
        "type " + spName + " = " + "\n    {" + props + "\n    }"
    let GenerateCode (sqlData: Generator) routineNames = 
        ""