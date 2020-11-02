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

type node_table = {
  nodeId:int;
  leaf_set:list<int>;
  neighbor_set:list<int>;
  routing_table: list<list<int>>;
}
let activeNode[] = Array.init<node_table> digits (fun x -> {nodeId = -1 ; leaf_set = []; neighbor_set =[]; routing_table=[[]]})
  
let L = 16
        
//Pastry_init -> Parent Actor
  //for loop 0 to nNodes-1
    //spawn - random and unique  within the range 0 - (digits-1)
    // update leaf set 
    // update neighbor set 
    // update routing table
  
  //When all nNodes are initialized, send a Message
  //Start sending files
  //for loop 0 to nNodes-1
        //while count < numRequests
             //randomly select a fileId from 0 - (digits - 1)
             //Call Routing Algo and store the number of hops returned
             
  //All the nodes have sent "numRequest" files
  // Calculate Average Hops and Terminate      
          
          
          
//update leaf set (nodeId)
let update_leaf_set nodeId =
    //look for L/2 active nodes (with nodeId not -1) on left and L/2 active nodes on right
    let mutable left = L/2
    while left > 0 do
        for i in nodeId-1 .. 0 do
            if nodeId <> -1 then
                //add node to
            
        
    //for each node in the LeafSet of the current node
        //update leaf set
  
    
    
    
//update neighbor set(nodeId)
    // same as leaf set
    
    
    
//update routing table(nodeId)
    // Prefix Matching for every active node ?
    
    
    
//Prefix Match Function(nodeID)
    //returns the number of digits in the common prefix


//Routing Algorithm
    //As per algo in the paper
    
    //return number of Hops
    
    
//Calculate Average Hops
    //sum of all the entries in the global array of number of hops