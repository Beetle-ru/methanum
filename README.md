**Methanum** is simple for use a .NET library for creating distributed systems with start topology.

First you should start the core in command prompt «Core.exe 2255».

Then client applications can send and receive messages. For this in a program code of a client application you should create connection:

```C#
var maingate = new Connector("localhost:2255");
```
You can create and send an event:
```C#
var evt = new Event("message");
evt.SetData("name", userName);
evt.SetData("text", msg);
maingate.Fire(evt);
```
If you want receive and handle messages you should set message handlers:
```C#
maingate.SetHandler("message", MsgHandler);
```
