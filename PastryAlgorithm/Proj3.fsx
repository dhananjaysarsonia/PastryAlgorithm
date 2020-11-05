// Learn more about F# at http://fsharp.org
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
open System
open System.Collections
open System.Runtime.CompilerServices
open System.Threading.Tasks
open Akka
open Akka.Actor
open Akka.Dispatch.SysMsg
open System.Collections.Generic
open Akka.FSharp
open Akka.Actor
open System.Diagnostics
open Akka.Util
open System.Threading;

let args = fsi.CommandLineArgs

let mutable nNodes = System.Int32.Parse(args.[1])
//let nNodes = 100
let nRequest = System.Int32.Parse(args.[2])
//let nRequest = 2

//Array to store hop counts for each request
let totalRequests = nNodes * nRequest
let mutable hopCountArray = Array.zeroCreate  nNodes
let liveActor : bool[] = Array.zeroCreate nNodes

let random = new System.Random()        
let digits = int <| ceil(Math.Log(float <| nNodes) / Math.Log(float <| 16))
let mutable hashActorMap = new Dictionary<string, IActorRef>()
let col = 16
type Message =
    | Initialize of String
    | Joining of String*int
    | UpdateRouting of String[]*int
    | UpdateLeaves
    | Route of String*int*String
    | UpdateLeaf of String*String[]*String[]
    | IMadeYouLeaf of String
    | Done of int*String*String
    | StartSending
    | MasterStart
    | JoiningDone of String

    
    
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
    let diff1 = abs (source - destination)
    let diff2 = abs (newNodeId - destination)
    let mutable resId = sourceId
    if diff2 < diff1 then
        resId <- newId
    resId
    
    
//let calculateAvg =
//    let mutable sum = 0
//    for i in 0 .. totalRequests-1 do
//        sum <- sum + hopCountArray.[i]
//    let averageCount = sum / totalRequests
//    averageCount
//    
    
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
    
let getSizeOfLeaves(leaves: String[]) =
    let mutable size = 0
    for i in leaves do
        if isNotNullString i then
            size <- size + 1
    
    size

let getEmptyIndexInLeaves(leaves : String[]) =
    let mutable index = 0
    let mutable returnIndex = 0
    for i in leaves do
        if String.IsNullOrEmpty i then
            returnIndex <- index
        index <- index + 1
        
    returnIndex
    
    
let decToHexConverted (i : int) =
    let mutable hexID = decToHex i
    let length = hexID.Length
    let mutable rem = digits - length
    while rem > 0 do
        hexID <- "0" + hexID
        rem <- rem - 1
        
    hexID    

    
let peer(mailbox : Actor<_>) =
    let mutable peerId = ""
    let mutable selfDecId = -1
    let mutable routingTable : string [,] = Array2D.zeroCreate 0 0
    let mutable smallLeafArray : String[] = [||]
    let mutable bigLeafArray : String[] = [||]
    let mutable requestsToSend = 0
    
    let rec loop() = actor{
        let! message = mailbox.Receive ()
        match message with
        |Initialize(id) ->
            peerId <- id
            printf "My peer id is %A \n" peerId
            requestsToSend <- nRequest
            selfDecId <- hexToDec peerId
            routingTable <- Array2D.zeroCreate digits col
//            let decId = hexToDec <| peerId
            smallLeafArray <- Array.create 8 ""
            bigLeafArray <- Array.create 8 ""

        
        | Joining(hashKey, rowIndex) ->
                
            if String.Compare(hashKey, peerId) <> 0 then
                Thread.Sleep(100)
                printf "joining for node %A and joining attempt with %A \n" hashKey peerId
                printf "bigleafset for peerid %A is %A" peerId bigLeafArray
                printf "smallLeafSet for peerId %A is %A" peerId smallLeafArray
                printf "Routing table for peer where join is attempted is %A %A \n" peerId routingTable
    //  
                let prefixMatch = getPrefixMatch peerId hashKey
                let mutable rIndex = 0
                if prefixMatch = routingTable.Length then
                    printf "ALERT!!!!!"
                
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
                    
                let routingColumn = hexToDec (string <| (hashKey.[prefixMatch]))
                if ((isSmaller hashKey maxLeafHash) && (isGreater hashKey minLeafHash)) || minLeafHash = peerId || maxLeafHash = peerId  then
                    //route towards nearest
                    let mutable nearest = peerId
                    for i in smallLeafArray do
                        if isNotNullString i then
                            nearest <- closestNode nearest hashKey i
                    
                    for i in bigLeafArray do
                        if isNotNullString i then
                            nearest <- closestNode nearest hashKey i
                    //ignored for self check 
                    
                    if String.Compare(nearest, peerId) <> 0 then
                        hashActorMap.Item(nearest) <! Joining(hashKey, 0)
                        
                    else
                        //termination logic
                        //try me as leaf
                        
                        hashActorMap.Item(hashKey) <! UpdateLeaf(peerId, smallLeafArray, bigLeafArray)
                        mailbox.Context.Parent <! JoiningDone(hashKey)
                        //
                    
                else
                    //routing table check
                    
                    if isNotNullString routingTable.[prefixMatch, routingColumn] then
                        hashActorMap.Item(routingTable.[prefixMatch, routingColumn]) <! Joining(hashKey, 0)
                    else
                        //search everything to find a nearest node
                        //rare case handle
                        
                        printf "RARE CASE ENTER" 
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
                            mailbox.Context.Parent <! JoiningDone(hashKey)
                            
                  //self table updation logic
                
                if String.IsNullOrEmpty routingTable.[prefixMatch, routingColumn] then
                    routingTable.[prefixMatch, routingColumn] <- hashKey
                else
                    let currentNode = routingTable.[prefixMatch, routingColumn]
                    routingTable.[prefixMatch, routingColumn] <- closestNode currentNode peerId hashKey
            else
                 mailbox.Context.Parent <! JoiningDone(hashKey)
                
                
            
                
        |UpdateRouting(row, rowIndex) ->
            for i in 0 .. routingTable.GetLength(0) do
                if isNotNullString row.[i] && String.Compare(row.[i], peerId) <> 0 then
                    let newRow = getPrefixMatch peerId row.[i]
                    if String.IsNullOrEmpty routingTable.[newRow, i] then
                        routingTable.[newRow, i] <- row.[i]
                        
                    else
                        routingTable.[newRow, i] <- closestNode routingTable.[newRow, i] peerId row.[i]
            
            printf "Routing table for %A is %A \n" peerId routingTable
        
        |UpdateLeaf(sourceId, sourceSmallLeafArray, sourceBigLeafArray) ->
            let mutable combinedSourceLeaf : String[] = Array.zeroCreate 17
            combinedSourceLeaf.[0] <- sourceId
            let mutable index = 1
            for i in sourceSmallLeafArray do
                if isNotNullString i then
                    combinedSourceLeaf.[index] <- i
                    index <- index + 1
            
            for i in sourceBigLeafArray do
                if isNotNullString i then
                    combinedSourceLeaf.[index] <- i
                    index <- index + 1
                    
            //check if i is maybe part of the leaves already
            for i in combinedSourceLeaf do
                if isNotNullString i then
                    if i <> peerId then
                        if isSmaller i peerId then
                            let size = getSizeOfLeaves smallLeafArray
                            if size < 8 then
                                let index = getEmptyIndexInLeaves smallLeafArray
                                smallLeafArray.[index] <- i
                                hashActorMap.Item(i) <! IMadeYouLeaf(peerId)
                            else
                                let minIndex = getMinFromLeaf smallLeafArray
                                if isSmaller smallLeafArray.[minIndex] i then
                                    smallLeafArray.[minIndex] <- i
                                    hashActorMap.Item(i) <! IMadeYouLeaf(peerId)
                        else
                            let size = getSizeOfLeaves bigLeafArray
                            if size < 8 then
                                let index = getEmptyIndexInLeaves bigLeafArray
                                bigLeafArray.[index] <- i
                                hashActorMap.Item(i) <! IMadeYouLeaf(peerId)
                            else
                                let maxIndex = getMaxFromLeaf bigLeafArray
                                if isGreater bigLeafArray.[maxIndex] i then
                                    bigLeafArray.[maxIndex] <- i
                                    hashActorMap.Item(i) <! IMadeYouLeaf(peerId)
                                    
//            printf "SmallLeaf table for %A is %A \n" peerId smallLeafArray
//            printf "LargeLeaf table for %A is %A \n" peerId smallLeafArray
                                
                                
        | IMadeYouLeaf(sourceId) ->
                let i = sourceId
                if i <> peerId then
                    if isSmaller i peerId then
                        let size = getSizeOfLeaves smallLeafArray
                        if size < 8 then
                            let index = getEmptyIndexInLeaves smallLeafArray
                            smallLeafArray.[index] <- i
                            //hashActorMap.Item(i) <! IMadeYouLeaf(peerId)
                        else
                            let minIndex = getMinFromLeaf smallLeafArray
                            if isSmaller smallLeafArray.[minIndex] i then
                                smallLeafArray.[minIndex] <- i
                                //hashActorMap.Item(i) <! IMadeYouLeaf(peerId)
                    else
                        let size = getSizeOfLeaves bigLeafArray
                        if size < 8 then
                            let index = getEmptyIndexInLeaves bigLeafArray
                            bigLeafArray.[index] <- i
                            //hashActorMap.Item(i) <! IMadeYouLeaf(peerId)
                        else
                            let maxIndex = getMaxFromLeaf bigLeafArray
                            if isGreater bigLeafArray.[maxIndex] i then
                                bigLeafArray.[maxIndex] <- i
                                //hashActorMap.Item(i) <! IMadeYouLeaf(peerId)
                
        | Route(key,hopCount, origin) ->
            
            if key <> peerId then
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
                
                if ((isSmaller key maxLeafHash) && (isGreater key minLeafHash)) || (isEqual key maxLeafHash) || (isEqual key minLeafHash) then
                    //route towards nearest
                    let mutable nearest = peerId
                    for i in smallLeafArray do
                        if isNotNullString i then
                            nearest <- closestNode nearest key i
                    
                    for i in bigLeafArray do
                        if isNotNullString i then
                            nearest <- closestNode nearest key i
                    //ignored for self check 
                    
                    if String.Compare(nearest, peerId) <> 0 then
                        hashActorMap.Item(nearest) <! Route(key, hopCount + 1, origin)
                    else
                        //I am taking it as the key
                        mailbox.Context.Parent <! Done(hopCount, key, origin)
                else
                    //routing table check
                    let prefixMatch = getPrefixMatch peerId key
                    let routingColumn = hexToDec (string <| (key.[prefixMatch]))
                    if isNotNullString routingTable.[prefixMatch, routingColumn] then
                        hashActorMap.Item(routingTable.[prefixMatch, routingColumn]) <! Route(key, hopCount + 1, origin)
                    else
                        //search everything to find a nearest node
                    //rare case handle 
                        let mutable rareCaseNode = peerId
                        for i in bigLeafArray do
                            if isNotNullString i then
                                rareCaseNode <- closestNode peerId key i
                                
                        for i in smallLeafArray do
                            if isNotNullString i then
                                rareCaseNode <- closestNode peerId key i
                                
                        for v in Seq.cast<String> routingTable do
                            if isNotNullString v then
                                rareCaseNode <- closestNode peerId key v
                        
                        if String.Compare(peerId, rareCaseNode) <> 0 then
                            hashActorMap.Item(rareCaseNode) <! Route(key, hopCount + 1, origin)
                        else
                            //termination logic repeated one as above
                            mailbox.Context.Parent <! Done(hopCount, key, origin)
            else
                mailbox.Context.Parent <! Done(hopCount, key, origin)
                
                
        | StartSending ->
            requestsToSend <- requestsToSend - 1
            let mutable flag = true
            while flag do
                let node = random.Next(nNodes)
                if nNodes <> selfDecId then
                    let hexKey = decToHexConverted node
                    mailbox.Self <! Route(hexKey, 0, peerId)
                    flag <- false
                    
            if requestsToSend > 0 then
                mailbox.Self <! StartSending
       
                
            
                
                
//            
        
        return! loop()
    }
    loop()
    

let system = System.create "system" (Configuration.defaultConfig())

let mutable doneCount = totalRequests
let mutable nodeComplete = nNodes    
let master(mailbox : Actor<_>) =
    let mutable notFirst = false
    let rec loop() = actor{
        let! message = mailbox.Receive()
        match message with
        
        
        | MasterStart ->
            let mutable aliveFound = false
            while not aliveFound do
                
                //while count < nNodes do
                let randNode = random.Next(nNodes)
                
                
                if not liveActor.[randNode] then
                    aliveFound <- true
                    let mutable hexID = decToHexConverted randNode
                    //printf "%A" hexID
                   // printf "Creating node %A \n" hexID 
                    
                    let child = spawn mailbox hexID peer
                    child <! Initialize(hexID)
                    //find node to join
                    let mutable flag = true
                    
                    while flag && notFirst do
                       // printf "%A" randNode
                        let joinNode = random.Next(nNodes)
                        if liveActor.[joinNode] then
                            let joinHexId = decToHexConverted joinNode
                            hashActorMap.Item(joinHexId) <! Joining(hexID, 0)
                            //Thread.Sleep(100)
                            flag <- false
                            
                    liveActor.[randNode] <- true
                    hashActorMap.Add(hexID, child)
                    if not notFirst then
                        notFirst <- true  
                        mailbox.Self <! MasterStart
                     
                    
                    
                   
                    
                    
                    
                    
            //initilization of network done
           
                
                
        | Done(hopCount, key, origin)->
            doneCount <- doneCount - 1
            let originDec = hexToDec origin
            hopCountArray.[originDec] <- hopCountArray.[originDec] + hopCount
            
            if doneCount = 0 then
                //let's calculate average here
                let mutable sum = 0
                for i in hopCountArray do
                    sum <- sum + i
                
                let average = (float <| sum) / (float <| totalRequests)
                printf "Average hopCount: %A" average
           
           
        | JoiningDone(idOfJoined) ->
            nodeComplete <- nodeComplete - 1
            printf "Nodes Left %A \n" nodeComplete
            if nodeComplete = 1 then
                printf "LET's START ROUTING OF KEYS"
                for kv in hashActorMap do
                    kv.Value <! StartSending
            else
                mailbox.Context.Self <! MasterStart
                
        
        return! loop()
    }
    loop()
    
    
let parent = spawn system "master" master
parent <! MasterStart


System.Console.ReadLine() |> ignore
















