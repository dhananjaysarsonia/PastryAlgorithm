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

let range = 200

//Array to store hop counts for each request
let totalRequests = nNodes * nRequest
let mutable hopCountArray = Array.create totalRequests 0
    
let mutable aliveActors = Array.create range false     

let random = new System.Random()
let digits = int <| ceil(Math.Log(float <| nNodes) / Math.Log(float <| 16))
let hashActorMap : System.Collections.Generic.IDictionary<String, IActorRef>  = dict[]
let col = 16
type Message =
    | Start
    | Initialize of String
    | Joining of String*int
    | UpdateRouting of String[]*int
    | UpdateLeaves
    | Route of String*String*int
    | UpdateLeaf of String*String[]*String[]
    | IMadeYouLeaf of String
    
    
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
        sum <- sum + hopCountArray.[i]
    let averageCount = sum / totalRequests
    averageCount
    
    
let getMinFromLeaf (smallSet : String[]) =
    let mutable m :int = Int32.MaxValue
    let mutable index = -1
    
    for i in 0 .. (smallSet.Length - 1) do
        if not (String.IsNullOrEmpty smallSet.[i]) then
            let value = hexToDec smallSet.[i]
            if m > value then
                m <- value
                index <- i
    index
    
    
    
let getMaxFromLeaf (largeSet : String[]) =
    let mutable m :int = Int32.MinValue
    let mutable index = -1
    
    for i in 0 .. (largeSet.Length - 1) do
        if not (String.IsNullOrEmpty largeSet.[i]) then
            let value = hexToDec largeSet.[i]
            if m < value then
                m <- value
                index <- i
    index
    
    

let isDistinctInArray (leaf : String[]) (value : String) =
    let mutable flag = true
    for i in leaf do
        if i = value then
            flag <- false
    flag
   

let isGreater (l: String) (r : String) =
    let lValue = hexToDec l
    let rValue = hexToDec r
    lValue > rValue
    
let isSmaller (l: String) (r : String) =
    let lValue = hexToDec l
    let rValue = hexToDec r
    lValue < rValue
    
let isEqual (l: String) (r : String) =
    let lValue = hexToDec l
    let rValue = hexToDec r
    lValue = rValue

let isNotNullString (str : String) =
    not (String.IsNullOrEmpty str)
             
let peer(mailbox : Actor<_>) =
    let mutable peerId = ""
    let mutable routingTable : string [,] = Array2D.zeroCreate 0 0
    let mutable smallLeafArray : String[] = [||]
    let mutable bigLeafArray : String[] = [||]
    
    let rec loop() = actor{
        let! message = mailbox.Receive ()
        match message with
        |Initialize(id) ->
            peerId <- id
            routingTable <- Array2D.zeroCreate digits col
            let decId = hexToDec <| peerId
            smallLeafArray <- Array.create 8 ""
            bigLeafArray <- Array.create 8 ""

        
        | Joining(hashKey, rowIndex) ->
  
            let prefixMatch = getPrefixMatch peerId hashKey
            let mutable rIndex = 0
            
            while rIndex <= prefixMatch do
                let mutable copiedRow : String[] = deepCopyRow routingTable rIndex
                copiedRow.[hexToDec (string <| (peerId.[prefixMatch]))] <- peerId
                hashActorMap.Item(hashKey) <! UpdateRouting(copiedRow,rIndex)
                rIndex <- rIndex + 1
          
            //now routing and termination logic
            
            //check leaves first
            let mutable maxLeafIndex = getMaxFromLeaf bigLeafArray
            let mutable smallLeafIndex = getMinFromLeaf smallLeafArray
            
            let mutable maxLeafHash = ""
            let mutable minLeafHash = ""
            if maxLeafIndex <> -1 then
                maxLeafHash <- bigLeafArray.[maxLeafIndex]
            if smallLeafIndex <> -1 then
                minLeafHash <- smallLeafArray.[smallLeafIndex]
            
            if String.IsNullOrEmpty maxLeafHash then
                maxLeafHash <- peerId
            
            if String.IsNullOrEmpty minLeafHash then
                minLeafHash <- peerId
                
            let routingColumn = hexToDec (string <| (peerId.[prefixMatch]))
            if (isSmaller hashKey maxLeafHash) & (isGreater hashKey minLeafHash) then
                //route towards nearest
                let mutable nearest = peerId
                for i in smallLeafArray do
                    if isNotNullString i then
                        nearest <- closestNode nearest hashKey i
                
                for i in bigLeafArray do
                    if isNotNullString i then
                        nearest <- closestNode nearest hashKey i
                
                if String.Compare(nearest, peerId) <> 0 then
                    hashActorMap.Item(nearest) <! Joining(hashKey, 0)
                    
                else
                    //termination logic
                    //try me as leaf
                    
                    hashActorMap.Item(hashKey) <! UpdateLeaf(peerId, smallLeafArray, bigLeafArray)
                    //
                
            else
                //routing table check
                if isNotNullString routingTable.[prefixMatch, routingColumn] then
                    hashActorMap.Item(hashKey) <! Joining(hashKey, 0)
                else
                    //search everything to find a nearest node
                    //rare case handle 
                    let mutable rareCaseNode = peerId
                    for i in bigLeafArray do
                        if isNotNullString i then
                            rareCaseNode <- closestNode peerId hashKey i
                            
                    for i in smallLeafArray do
                        if isNotNullString i then
                            rareCaseNode <- closestNode peerId hashKey i
                            
                    for v in Seq.cast<String> routingTable do
                        if isNotNullString v then
                            rareCaseNode <- closestNode peerId hashKey v
                    
                    if String.Compare(peerId, rareCaseNode) <> 0 then
                        hashActorMap.Item(rareCaseNode) <! Joining(hashKey, 0)
                    else
                        //termination logic repeated one as above
                        hashActorMap.Item(hashKey) <! UpdateLeaf(peerId, smallLeafArray, bigLeafArray)
            
              //self table updation logic
            
            if String.IsNullOrEmpty routingTable.[prefixMatch, routingColumn] then
                routingTable.[prefixMatch, col] <- hashKey
            else
                let currentNode = routingTable.[prefixMatch, col]
                routingTable.[prefixMatch, col] <- closestNode currentNode hashKey peerId
                   
        |UpdateRouting(row, rowIndex) ->
            for i in 0 .. routingTable.GetLength(0) do
                if String.IsNullOrEmpty routingTable.[rowIndex, i] then
                    routingTable.[rowIndex, i] <- row.[i]
                    
                else
                    routingTable.[rowIndex, i] <- closestNode routingTable.[rowIndex, i] row.[i] peerId 
        
        return! loop()
    }
    loop()

let nodeId: String = String.replicate digits "0"
printf "%A" nodeId

//let system = System.create "system" (Configuration.defaultConfig())
//let pastry = spawn system "pastry" pastryActor
//pastry <! Start
















