using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using UnityEngine;
using ValheimPlus;
using ValheimPlus.RPC;
using ValheimPlus.GameClasses;

public static class PeerHost {
 public static Action<long,long,string,byte[]> Send;public static List<string> Logs=new();
 public static void Init(long id,bool server,string path){ZNet.instance=new ZNet{Server=server};foreach(long peer in new long[]{2,3,4})ZNet.instance.Peers[peer]=new ZNetPeer{m_socket=new Socket{Account=peer==4?"admin":"account"+peer}};ZRoutedRpc.instance=new ZRoutedRpc{Id=id};Player.m_localPlayer=server?null:new Player();Minimap.instance=new Minimap();ValheimPlusPlugin.VPlusDataDirectoryPath=path;VPlusSharedPins.Start();}
 public static void Receive(long sender,string method,byte[] bytes){ZRoutedRpc.instance.Handlers[method](sender,new ZPackage(bytes));}
 public static void Tick(){Time.realtimeSinceStartup+=0.25f;VPlusSharedPins.Update();}
 public static bool Ready()=>VPlusSharedPins.Ready;
 public static int Shared()=>VPlusSharedPins.Displayed.Count;
 public static bool Has(string n)=>Minimap.instance.m_pins.Any(p=>p.m_name==n);
 public static bool Checked(string n)=>Minimap.instance.m_pins.Single(p=>p.m_name==n).m_checked;
 static object Call(Type t,string n,params object[] args)=>t.GetMethod(n,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
 public static void Create(string name,bool ctrl){ZInput.Pressed=ctrl?KeyCode.LeftControl:null;var map=Minimap.instance;map.m_namePin=map.AddPin(new Vector3(10,20,30),Minimap.PinType.Icon0,"",true,false);Call(typeof(SharedPinCreation),"Postfix",map);map.m_namePin.m_name=name;var a=new object[]{map,null};Call(typeof(SharedPinFinishName),"Prefix",a);map.m_namePin=null;Call(typeof(SharedPinFinishName),"Postfix",a[1]);ZInput.Pressed=null;}
 public static void Publish(string name){ZInput.Pressed=KeyCode.LeftShift;Minimap.instance.Closest=Minimap.instance.m_pins.Single(p=>p.m_name==name);Call(typeof(PublishPrivatePin),"Prefix",Minimap.instance);ZInput.Pressed=null;}
 public static void Delete(string name){var p=Minimap.instance.m_pins.Single(p=>p.m_name==name);if(VPlusSharedPins.Remove(p))Minimap.instance.RemovePin(p);}
 public static void Toggle(string name){var p=Minimap.instance.m_pins.Single(p=>p.m_name==name);p.m_checked=!p.m_checked;}
 public static string SaveView(){var a=new object[]{null};Call(typeof(ExcludeSharedPinsFromCharacterSave),"Prefix",a);var names=string.Join(",",Minimap.instance.m_pins.Where(p=>p.m_save).Select(p=>p.m_name));Call(typeof(ExcludeSharedPinsFromCharacterSave),"Finalizer",a);return names;}
 public static bool AllSelectable()=>Minimap.instance.m_pins.All(p=>p.m_save);
 public static void Busy(long id,int size)=>ZNet.instance.Peers[id].m_socket.Queue=size;
 public static void Stop()=>VPlusSharedPins.Reset(); public static void Host(){ZNet.instance.Dedicated=false;Player.m_localPlayer=new Player();}
 public static void Conflict(){BepInEx.Bootstrap.Chainloader.PluginInfos["txc"]=new BepInEx.Bootstrap.PluginInfo{Metadata=new BepInEx.Bootstrap.Metadata{GUID="txc.sharedmap"}};}
 public static bool Enabled()=>VPlusSharedPins.Enabled;
 public static string Errors()=>string.Join("\n",Logs.Where(s=>s.Contains("retry")||s.Contains("failed")||s.Contains("invalid")));
}
class Peer:IDisposable {
 readonly AssemblyLoadContext context;readonly Type api;
 public Peer(long id,bool server,string dir,Action<long,long,string,byte[]> send){context=new AssemblyLoadContext("peer"+id,true);api=context.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location).GetType("PeerHost");api.GetField("Send").SetValue(null,send);Call("Init",id,server,dir);}
 public object Call(string n,params object[] a)=>api.GetMethod(n).Invoke(null,a);
 public T Get<T>(string n,params object[] a)=>(T)Call(n,a);
 public void Dispose(){Call("Stop");context.Unload();}
}
class Program {
 static int count;static void Check(bool yes,string name){if(!yes)throw new Exception("FAIL "+name);count++;Console.WriteLine("PASS "+name);}
 static SharedPin Pin(string name="Copper")=>new(){Id=Guid.NewGuid().ToString("N"),Name=name,Type=0,X=1,Y=2,Z=3,Owner="forged"};
 static void Main(){try{Run();Console.WriteLine($"{count} checks passed");}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){
 string dir=Path.Combine(Directory.GetCurrentDirectory(),"artifacts","shared-pin-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
 var store=new SharedPinStore();var pin=Pin();Check(store.Add(pin,"alice")&&store.Pins[pin.Id].Owner=="alice","server supplies owner, ignoring claimed owner");
 Check(store.Add(pin,"alice")&&store.Revision==1,"duplicate creation is idempotent");Check(!store.Add(pin,"bob"),"another account cannot take pin ID");Check(!store.Remove(pin.Id,"bob",false),"non-owner delete rejected");Check(store.Check(pin.Id,true)&&store.Pins[pin.Id].Checked,"check state changes");
 string path=Path.Combine(dir,"store.dat");store.Save(path,42);var loaded=SharedPinStore.Load(path,42);Check(loaded.Pins[pin.Id].Checked&&loaded.Revision==2,"pin and revision survive restart");
 Check(loaded.Remove(pin.Id,"admin",true),"admin can delete");loaded.Save(path,42);loaded=SharedPinStore.Load(path,42);Check(!loaded.Add(pin,"alice"),"persisted deletion rejects replayed creation");Check(File.Exists(path+".bak"),"atomic replacement keeps backup");
 bool rejected=false;try{SharedPinStore.Load(path,99);}catch(InvalidDataException){rejected=true;}Check(rejected,"world mismatch rejected");var bad=Pin();bad.X=float.NaN;Check(!store.Add(bad,"alice"),"non-finite position rejected");bad=Pin(new string('a',129));Check(!store.Add(bad,"alice"),"oversize name rejected");bad=Pin();bad.Type=4;Check(!store.Add(bad,"alice"),"death pin not shared");bad=Pin("<b>bad</b>");Check(!store.Add(bad,"alice"),"markup rejected");
 var snapshot=store.Copy();snapshot.Pins[pin.Id].Name="changed";Check(store.Pins[pin.Id].Name=="Copper","uncommitted changes cannot mutate live store");
 File.WriteAllBytes(Path.Combine(dir,"corrupt.dat"),new byte[]{1,2,3});rejected=false;try{SharedPinStore.Load(Path.Combine(dir,"corrupt.dat"),42);}catch(EndOfStreamException){rejected=true;}Check(rejected,"truncated save rejected without replacing it");
 var queue=new Queue<(long from,long to,string method,byte[] data)>();var peers=new Dictionary<long,Peer>();Action<long,long,string,byte[]> send=(from,to,m,d)=>queue.Enqueue((from,to,m,d));
 void Deliver(){int max=10000;while(queue.Count>0){if(--max==0)throw new Exception("RPC echo loop");var msg=queue.Dequeue();if(peers.TryGetValue(msg.to,out var peer))peer.Call("Receive",msg.from,msg.method,msg.data);}}
 void Pump(int ticks=8){for(int i=0;i<ticks;i++){foreach(var peer in peers.Values.ToList())peer.Call("Tick");Deliver();}}
 peers[1]=new Peer(1,true,dir,send);peers[2]=new Peer(2,false,dir,send);Pump();Check(peers[2].Get<bool>("Ready"),"dedicated server handshake and empty snapshot");
 peers[2].Call("Create","Private",true);Pump();Check(peers[2].Get<int>("Shared")==0,"Ctrl pin stays private");
 peers[2].Call("Create","Public",false);Pump();Check(peers[2].Get<int>("Shared")==1&&peers[2].Get<bool>("Has","Public"),"default public creation uses finalized name");
 peers[3]=new Peer(3,false,dir,send);Pump();Check(peers[3].Get<int>("Shared")==1&&!peers[3].Get<bool>("Has","Private"),"late join receives public pins only");
 peers[3].Call("Delete","Public");Pump();Check(peers[2].Get<bool>("Has","Public")&&peers[3].Get<bool>("Has","Public"),"server rejects non-owner deletion");
 peers[3].Call("Toggle","Public");Pump();Check(peers[2].Get<bool>("Checked","Public")&&peers[3].Get<bool>("Checked","Public"),"check marks synchronize to owner and peer");
 Check(peers[2].Get<string>("SaveView")=="Private"&&peers[2].Get<bool>("AllSelectable"),"character/cartography serialization excludes public pins and restores hit testing");
 peers[2].Call("Publish","Private");Pump();Check(peers[2].Get<int>("Shared")==2&&peers[3].Get<bool>("Has","Private"),"Shift-click publishes existing private pin without local duplicate");
 peers[1].Call("Busy",2L,6000);peers[3].Call("Toggle","Public");Pump();Check(peers[2].Get<bool>("Checked","Public")&&!peers[3].Get<bool>("Checked","Public"),"busy connection pauses pin traffic only for that peer");peers[1].Call("Busy",2L,0);Pump();Check(!peers[2].Get<bool>("Checked","Public"),"queued state catches up after congestion");
 peers[2].Call("Delete","Public");Pump();Check(!peers[2].Get<bool>("Has","Public")&&!peers[3].Get<bool>("Has","Public"),"owner deletion reaches both clients");
 peers[1].Dispose();peers[1]=new Peer(1,true,dir,send);peers[3].Dispose();peers[3]=new Peer(3,false,dir,send);Pump(24);Check(peers[3].Get<int>("Shared")==1&&peers[3].Get<bool>("Has","Private")&&!peers[3].Get<bool>("Has","Public"),"server restart and reconnect preserve pins and deletions");
 peers[4]=new Peer(4,false,dir,send);Pump();peers[4].Call("Delete","Private");Pump();Check(peers[2].Get<int>("Shared")==0&&peers[3].Get<int>("Shared")==0,"admin deletion reaches other clients");
 for(int i=0;i<35;i++){peers[3].Call("Create","Batch"+i,false);Pump(2);}
 peers[4].Dispose();peers[4]=new Peer(4,false,dir,send);peers[1].Call("Busy",4L,6000);Pump(4);peers[3].Call("Create","DuringSnapshot",false);Pump(2);peers[1].Call("Busy",4L,0);Pump(8);Check(peers[4].Get<int>("Shared")==36&&peers[4].Get<bool>("Has","DuringSnapshot"),"multi-page initial snapshot followed by queued live mutation converges");
 peers[1].Call("Host");Pump(16);Check(peers[1].Get<bool>("Ready")&&peers[1].Get<int>("Shared")==36,"listen host receives the server snapshot on its own peer");peers[1].Call("Create","HostPin",false);Pump();Check(peers[4].Get<bool>("Has","HostPin"),"listen host can publish through the same authoritative protocol");peers[1].Call("Delete","HostPin");Pump();Check(!peers[4].Get<bool>("Has","HostPin"),"listen host delete is synchronized");
 peers[2].Call("Conflict");Check(!peers[2].Get<bool>("Enabled"),"TXC conflict disables V+ pin handling");
 Check(peers[1].Get<string>("Errors")==""&&peers[3].Get<string>("Errors")=="","protocol integration has no retries or invalid packets"); Directory.CreateDirectory(Path.Combine(dir,"42_sharedPins.dat.tmp"));peers[3].Call("Create","DiskFailure",false);Pump();Check(peers[3].Get<bool>("Has","DiskFailure")&&peers[3].Get<int>("Shared")==36&&!peers[4].Get<bool>("Has","DiskFailure"),"failed durable commit leaves source private and does not broadcast");
 foreach(var peer in peers.Values)peer.Dispose();
 }
}
