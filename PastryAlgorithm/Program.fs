// Learn more about F# at http://fsharp.org

open System
open System.Runtime.CompilerServices
open System.Threading.Tasks
open Akka
open Akka.Actor
open Akka.Dispatch.SysMsg
open Akka.FSharp
open Akka.Actor
open System.Diagnostics
open Akka.Util
open System.Threading;


let nNodes = 100
let nRequest = 5

//Array to store hop counts for each request
let totalRequests = nNodes * nRequest
let mutable hopCountArray = Array.create totalRequests 0
    
let digits = int <| ceil(Math.Log(float <| nNodes) / Math.Log(float <| 16))
let hashActorMap : System.Collections.Generic.IDictionary<String, IActorRef>  = dict[]
let col = 16
type Message =
    | Initialize of String
    | Joining of String*int
    | UpdateRouting of String[]*int
    | UpdateLeaves
    | Route of String*String*int
    
    
let hexToDec (hex: String) =
    Convert.ToInt32(hex, 16)

let decToHex(dec : int) =
    dec.ToString("X")
    
let getPrefixMatch (l : String) (r : String) =
    let mutable index = 0
    let mutable con = true
    while con do
        if l.[index] = r.[index] then
            index <- index + 1
        else
            con <- false
    index
    
let deepCopyRow (routingTable : string [,]) (rowIndex) =
    Array.copy routingTable.[rowIndex,*]
    
    
let peer(mailbox : Actor<_>) =
    let mutable peerId = ""
    let mutable routingTable : string [,] = Array2D.zeroCreate 0 0
    let mutable smallLeafSet = Set.empty
    let mutable bigLeafSet = Set.empty
    let rec loop() = actor{
        let! message = mailbox.Receive ()
        match message with
        |Initialize(id) ->
            peerId <- id
            routingTable <- Array2D.zeroCreate digits col
            let decId = hexToDec <| peerId
            
            for x in (decId - 1).. -1 .. (decId - 8) do
                if x >= 0 then 
                    smallLeafSet <- smallLeafSet.Add(x)
                else
                    smallLeafSet <- smallLeafSet.Add(nNodes - x)
                
            for x in (decId + 1) .. (decId + 1) do
                bigLeafSet <- bigLeafSet.Add(x%nNodes)
        
        | Joining(hashKey, rowIndex) ->
            
            let prefixMatch = getPrefixMatch peerId hashKey
            let mutable rIndex = rowIndex
            
            while rIndex <= prefixMatch do
                let mutable copiedRow : String[] = deepCopyRow routingTable rIndex
                copiedRow.[hexToDec (string <| (peerId.[prefixMatch]))] <- peerId
                hashActorMap.Item(hashKey) <! UpdateRouting(copiedRow,rIndex)
                rIndex <- rIndex + 1
                
                
           //time to update my row
            let routingColumn = hexToDec (string <| (peerId.[prefixMatch]))
            if String.IsNullOrEmpty routingTable.[prefixMatch, routingColumn] then
                routingTable.[prefixMatch, col] <- hashKey
            else
                hashActorMap.Item(routingTable.[prefixMatch,routingColumn]) <! Joining(hashKey, rIndex)
                
        |UpdateRouting(row, rowIndex) ->
            for i in 0 .. routingTable.GetLength(0) do
                if String.IsNullOrEmpty routingTable.[rowIndex, i] then
                    routingTable.[rowIndex, i] <- row.[i]
                
        
//        |Route(hashKey, hashSource, hopCount) ->
//            if String.Compare(hashKey, peerId)  = 0 then
//                //deliver here
//            else if 
//                
            
                
            
            
            
            
        
        
        
        return! loop()
    }
    loop()
    
    


let closestNode sourceId destId newId=
    let source = hexToDec sourceId
    let destination = hexToDec destId
    let newNodeId = hexToDec newId
    let diff1 = abs source - destination
    let diff2 = abs newNodeId - destination
    let mutable resId = sourceId
    if diff2 < diff1 then
        resId <- newId
    resId
    
    
let calculateAvg =
    let mutable sum = 0
    for i in 0 .. totalRequests-1 do
        sum <- sum + i
    let averageCount = sum / totalRequests
    averageCount
    

let nodeId: String = String.replicate digits "0"
printf "%A" nodeId


















