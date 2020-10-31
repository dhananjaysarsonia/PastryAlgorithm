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











