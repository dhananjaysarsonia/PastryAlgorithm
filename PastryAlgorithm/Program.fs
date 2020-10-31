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

let digits = int <| ceil(Math.Log(float <| nNodes) / Math.Log(float <| 16))
let col = 16
type Message =
    | Initialize of String
    | Joining
    | UpdateRouting
    | UpdateLeaves
    
    
let hexToDec (hex: String) =
    Convert.ToInt32(hex, 16)

let decToHex(dec : int) =
    dec.ToString("X")
    
let peer(mailbox : Actor<_>) =
    let mutable peerId = ""
    let mutable routingTable : string [,] = Array2D.zeroCreate 0 0
    let smallLeafSet = Set.empty
    let bigLeafSet = Set.empty
    let rec loop() = actor{
        let! message = mailbox.Receive ()
        match message with
        |Initialize(id) ->
            peerId <- id
            routingTable <- Array2D.zeroCreate digits col
            //let decId = hexToDec <| peerId
            
            
            
        
        
        
        return! loop()
    }
    loop()
    
    




let nodeId: String = String.replicate digits "0"
printf "%A" nodeId


















