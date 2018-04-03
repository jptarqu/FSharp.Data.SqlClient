namespace FSharp.Data

open System
open System.Collections.Generic
open System.Data
open System.Data.SqlClient
open System.Data.SqlTypes
open Microsoft.SqlServer.Server


open FSharp.Data.SqlClient




type ColDef =
    {
        Name: string
        ClrType: string
    }
type RowDef =
    {
        Name: string
        Cols: ColDef list
    }

type Generator(conn: SqlConnection, schema) =
    let GetOutputColumns (connection: SqlConnection, commandText, parameters: Parameter list, isStoredProcedure) = 
                try
                    connection.GetFullQualityColumnInfo(commandText) 
                with :? SqlException as why ->
                    try 
                        let commandType = if isStoredProcedure then CommandType.StoredProcedure else CommandType.Text
                        connection.FallbackToSETFMONLY(commandText, commandType, parameters) 
                    with :? SqlException ->
                        raise why
                            
    let generateRoutines() =
        use c = conn.UseLocally()
        let isSqlAzure = conn.IsSqlAzure
        let routines = conn.GetRoutines( schema, isSqlAzure) 
        routines 
    let generateRoutinesCodes routine =
        use c = conn.UseLocally()
        let isSqlAzure = conn.IsSqlAzure
        let parameters = conn.GetParameters( routine, isSqlAzure, true)
        
        let commandText = routine.ToCommantText(parameters)
        printfn "\n commandText %s" commandText
        
        let outputColumns = GetOutputColumns(conn, commandText, parameters, routine.IsStoredProc)
        let rank = if routine.Type = ScalarValuedFunction then ResultRank.ScalarValue else ResultRank.Sequence
        parameters, outputColumns
        
    let routines = generateRoutines()
    let routineNames = routines |> Seq.map (fun r -> (snd r.TwoPartName)) |> Seq.sort
    member this.RoutineNames
        with get() = routineNames 
    member this.GetRoutineParamsAndCols(routineName) =
        generateRoutinesCodes (routines |> Seq.find (fun r -> (snd r.TwoPartName) = routineName))
//        and private set(value) =
//            firstName <- value
//            let returnType = DesignTime.GetOutputTypes(outputColumns, resultType, rank, hasOutputParameters, unitsOfMeasurePerSchema)

//            do
//                SharedLogic.alterReturnTypeAccordingToResultType returnType cmdProvidedType resultType

            //ctors
//            let sqlParameters = Expr.NewArray( typeof<SqlParameter>, parameters |> List.map QuotationsFactory.ToSqlParam)
//
//            let designTimeConfig = 
//                let expectedDataReaderColumns = 
//                    Expr.NewArray(
//                        typeof<string * string>, 
//                        [ for c in outputColumns -> Expr.NewTuple [ Expr.Value c.Name; Expr.Value c.TypeInfo.ClrTypeFullName ] ]
//                    )
//
//                <@@ {
//                    SqlStatement = commandText
//                    IsStoredProcedure = %%Expr.Value( routine.IsStoredProc)
//                    Parameters = %%sqlParameters
//                    ResultType = resultType
//                    Rank = rank
//                    RowMapping = %%returnType.RowMapping
//                    ItemTypeName = %%returnType.RowTypeName
//                    ExpectedDataReaderColumns = %%expectedDataReaderColumns
//                } @@>
//
//            yield! DesignTime.GetCommandCtors(
//                cmdProvidedType, 
//                designTimeConfig, 
//                designTimeConnectionString,
//                config.IsHostedExecution
//            )
//
//            let executeArgs = DesignTime.GetExecuteArgs(cmdProvidedType, parameters, uddtsPerSchema, unitsOfMeasurePerSchema)
//
//            yield upcast DesignTime.AddGeneratedMethod(parameters, hasOutputParameters, executeArgs, cmdProvidedType.BaseType, returnType.Single, "Execute") 
//
//            if not hasOutputParameters
//            then
//                let asyncReturnType = ProvidedTypeBuilder.MakeGenericType(typedefof<_ Async>, [ returnType.Single ])
//                yield upcast DesignTime.AddGeneratedMethod(parameters, hasOutputParameters, executeArgs, cmdProvidedType.BaseType, asyncReturnType, "AsyncExecute")
//
//            if returnType.PerRow.IsSome
//            then
//                let providedReturnType = ProvidedTypeBuilder.MakeGenericType(typedefof<_ option>, [ returnType.PerRow.Value.Provided ])
//                let providedAsyncReturnType = ProvidedTypeBuilder.MakeGenericType(typedefof<_ Async>, [ providedReturnType ]) 
//
//                if not hasOutputParameters
//                then
//                    yield upcast DesignTime.AddGeneratedMethod(parameters, hasOutputParameters, executeArgs, cmdProvidedType.BaseType, providedReturnType, "ExecuteSingle") 
//                    yield upcast DesignTime.AddGeneratedMethod(parameters, hasOutputParameters, executeArgs, cmdProvidedType.BaseType, providedAsyncReturnType, "AsyncExecuteSingle")
//     