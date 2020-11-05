COP5615 - Fall 2020

November 04, 2020

#### Team Members

##### Dhananjay Sarsonia  UFID: 1927-5958

##### Forum Gala          UFID: 6635-6557


#### How to run
Using terminal, go to the directory containing the program.fsx file.
- Please make sure dotnet core is installed
- Run using the command: 

**dotnet fsi --langversion:preview program.fsx numNodes numRequests**
 
- numNodes  is the total number of nodes in the peer-to-peer system 
- numRequests is the total number of requests each node has to make


#### What is working?

A peer-to-peer network with "numNodes" number of nodes is initialized with a peerId, Leafset of size 16 nodes and Routing table. Once all the nodes are joined to the network, each node sends "numRequests" number of requests with randomly generated Ids and the number of hops are calculated for each request delivery. After all the nodes are done sending requests, the average number of hops is calculated and displayed.

#### What is the largest network you managed to deal with?

The largest network tested is for 10000 nodes with 10 requests per node.



##### Output

- numNodes - 10 and numRequests - 10

Average Hop Count = 1.01

- numNodes - 100 and numRequests - 10

Average Hop Count - 1.511

- numNodes - 500 and numRequests - 10

Average Hop Count - 2.16

- numNodes - 1000 and numRequests - 10 

Average Hop Count - 2.42

- numNodes - 2500 and numRequests - 10

Average Hop Count - 

- numNodes - 5000 and numRequests - 10

Average Hop Count - 

- numNodes - 10000 and numRequests - 10

Average Hop Count - 

