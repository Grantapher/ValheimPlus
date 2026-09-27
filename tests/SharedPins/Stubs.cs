using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace UnityEngine {
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c=0){x=a;y=b;z=c;}}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public enum KeyCode{LeftControl,LeftShift}
 public static class Input{public static bool GetKey(KeyCode key)=>throw new InvalidOperationException("Legacy Unity input must not be used for map modifiers");}
 public static class Time{public static float realtimeSinceStartup;}
 public class Image{public Color color;}
 public class Text{public Color color;}
}
public static class ZInput{public static bool Gamepad,Left;public static bool IsExclusiveGamepadActive()=>Gamepad;public static bool GetButtonDown(string n)=>Left && n=="JoyDPadLeft";public static UnityEngine.KeyCode? Pressed;public static bool GetKey(UnityEngine.KeyCode key,bool logWarning=true)=>Pressed==key;}
namespace HarmonyLib {
 public class HarmonyPatch:Attribute{public HarmonyPatch(){} public HarmonyPatch(Type t,string name){}public HarmonyPatch(Type t,string name,Type[] args){}}
 public class HarmonyPriority:Attribute{public HarmonyPriority(int p){}}
 public static class Priority{public const int First=800;}
 public static class AccessTools{public static MethodInfo DeclaredMethod(Type t,string n)=>t.GetMethod(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);}
}
namespace BepInEx.Bootstrap {
 public class Metadata{public string GUID;}
 public class PluginInfo{public Metadata Metadata=new Metadata();public object Instance;}
 public static class Chainloader{public static Dictionary<string,PluginInfo> PluginInfos=new();}
}
namespace ValheimPlus.Configurations {
 public class MapConfig{public bool IsEnabled=true,sharePins=true;public UnityEngine.KeyCode privatePinKey=UnityEngine.KeyCode.LeftControl,publishPinKey=UnityEngine.KeyCode.LeftShift;}
 public class Configuration{public static Configuration Current=new();public MapConfig Map=new();}
}
namespace ValheimPlus {public class Log{public void LogInfo(object s){PeerHost.Logs.Add(s.ToString());}public void LogWarning(object s){PeerHost.Logs.Add(s.ToString());}public void LogError(object s){PeerHost.Logs.Add(s.ToString());}public void LogDebug(object s){PeerHost.Logs.Add(s.ToString());}}
 public static class ValheimPlusPlugin{public static string VPlusDataDirectoryPath;public static Log Logger=new();}}
public class MessageHud{public enum MessageType{Center}}
public class Player{public static Player m_localPlayer;public void Message(MessageHud.MessageType t,string text){PeerHost.Logs.Add(text);}}
public class UserInfo{public string UserId="host";public static UserInfo GetLocalUser()=>new();}
public class Socket{public string Account;public int Queue;public string GetHostName()=>Account;public int GetSendQueueSize()=>Queue;}
public class ZNetPeer{public Socket m_socket=new();public bool IsReady()=>true;}
public class ZNet{
 public static ZNet instance;public bool Server,Dedicated=true;public Dictionary<long,ZNetPeer> Peers=new();
 public bool IsServer()=>Server;public bool IsDedicated()=>Dedicated;public long GetWorldUID()=>42;
 public ZNetPeer GetPeer(long id)=>Peers.TryGetValue(id,out var p)?p:null;public bool IsAdmin(string id)=>id=="admin";public void Update(){}public void Shutdown(){}
}
public class ZRoutedRpc {
 public static ZRoutedRpc instance;public long Id,ServerId=1;
 public Dictionary<string,Action<long,ZPackage>> Handlers=new();
 public void Register<T>(string name,Action<long,T> action){Handlers.Add(name,(s,p)=>action(s,(T)(object)p));}
 public long GetServerPeerID()=>ServerId;
 public void InvokeRoutedRPC(long target,string method,params object[] args){PeerHost.Send(Id,target,method,((ZPackage)args[0]).GetArray());}
}
public class ZPackage {
 readonly MemoryStream stream;readonly BinaryReader reader;readonly BinaryWriter writer;
 public ZPackage():this(Array.Empty<byte>()){}public ZPackage(byte[] bytes){stream=new MemoryStream();stream.Write(bytes);stream.Position=0;reader=new BinaryReader(stream);writer=new BinaryWriter(stream);}
 public int Size()=>(int)stream.Length;public byte[] GetArray()=>stream.ToArray();
 public void Write(int v)=>writer.Write(v);public void Write(long v)=>writer.Write(v);public void Write(float v)=>writer.Write(v);public void Write(bool v)=>writer.Write(v);public void Write(string v)=>writer.Write(v);
 public int ReadInt()=>reader.ReadInt32();public long ReadLong()=>reader.ReadInt64();public float ReadSingle()=>reader.ReadSingle();public bool ReadBool()=>reader.ReadBoolean();public string ReadString()=>reader.ReadString();
}
public class Minimap {
 public enum MapMode{None,Small,Large} public MapMode m_mode;public float inputDelay;public float PinInteractRadius=>1;public static bool TextInput;public static bool InTextInput()=>TextInput;public UnityEngine.GameObject m_largeRoot=new();public TMPro.TMP_Text m_biomeNameLarge=new();public UnityEngine.Vector3 ScreenToWorldPoint(UnityEngine.Vector3 p)=>p;public PinData GetClosestPin(UnityEngine.Vector3 p,float r)=>Closest;public void UpdateMap(){}public void SetMapMode(){} public enum PinType{Icon0,Icon1,Icon2,Icon3,Death,Bed,Icon4,Shout,None,Boss}
 public class PinNameData{public UnityEngine.Text PinNameText=new();}
 public class PinData{public string m_name;public PinType m_type;public UnityEngine.Vector3 m_pos;public bool m_save,m_checked;public long m_ownerID;public UnityEngine.Image m_iconElement=new();public PinNameData m_NamePinData;}
 public static Minimap instance;public List<PinData> m_pins=new();public PinData m_namePin,Closest;public bool m_pinUpdateRequired;
 public PinData AddPin(UnityEngine.Vector3 p,PinType t,string n,bool save,bool check){var pin=new PinData{m_pos=p,m_type=t,m_name=n,m_save=save,m_checked=check};m_pins.Add(pin);return pin;}
 public void RemovePin(PinData pin){m_pins.Remove(pin);}public PinData GetClosestPinToCursor()=>Closest;
 public void ShowPinNameInput(){}public void HidePinTextInput(){}public void OnMapLeftClick(){}public void UpdatePins(){}public void GetMapData(){}public void GetSharedMapData(){}
}
public class Game{public void Start(){}}

namespace UnityEngine {
 public class Object{public static void Destroy(object o){}}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public class Transform{public void SetParent(Transform t,bool b){}}
 public class RectTransform:Transform{public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta;}
 public class GameObject{public Transform transform=new();public GameObject(){}public GameObject(string s,params Type[] types){}public T AddComponent<T>() where T:new()=>new T();public void SetActive(bool b){}}
 public static class Screen{public static int width=1920,height=1080;}
}
namespace TMPro{
 public enum TextAlignmentOptions{Center}
 public class TMP_Text{public UnityEngine.GameObject gameObject=new();public object font;public float fontSize,outlineWidth;public bool raycastTarget;public TextAlignmentOptions alignment;public UnityEngine.Color color;public UnityEngine.RectTransform rectTransform=new();public string text;}
 public class TextMeshProUGUI:TMP_Text{}
}
