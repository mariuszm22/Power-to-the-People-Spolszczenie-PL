using System;using System.IO;using System.Text;using System.Linq;using System.Reflection;using System.Collections.Generic;
namespace Marko22.PTTP {
 class CorruptPatch:IPatches{readonly IPatches source;public CorruptPatch(IPatches p){source=p;}public byte[] Get(string n){var b=source.Get(n);b[12]^=1;return b;}}
 class TestProgram {
  static readonly List<string> results=new List<string>();static string root;static IPatches payload;static Engine engine;
  static string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n").Replace("\t","\\t")+"\"";}
  static void Check(bool v,string m){if(!v)throw new Exception(m);}
  static void Case(string name,Action action){try{action();results.Add("{\"name\":"+Q(name)+",\"passed\":true}");}catch(Exception e){results.Add("{\"name\":"+Q(name)+",\"passed\":false,\"error\":"+Q(e.Message)+"}");throw;}}
  static void Refused(Action action){bool refused=false;try{action();}catch(IOException){refused=true;}Check(refused,"Unsafe operation was not refused");}
  static void State(string s){Check(engine.Probe()==s,"Unexpected file state: "+engine.Probe());}
  static string Target(int n){return Path.Combine(root,Product.Entries[n].Path);}static string Backup(int n){return Path.Combine(root,Product.StateName,"original-"+n+".bin");}
  static int Main(){
   root=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"kopia_testowa"));
   Check(root.StartsWith(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd('\\','/')+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Test root outside workspace");
   payload=new EmbeddedPatches(Assembly.GetExecutingAssembly());engine=new Engine(root,payload,()=>false);int code=0;
   try{
    State("ORIGINAL");
    Case("Reject wrong folder",()=>Refused(()=>new Engine(AppDomain.CurrentDomain.BaseDirectory,payload,()=>false).Probe()));
    Case("Reject running-game guard",()=>{Refused(()=>new Engine(root,payload,()=>true).Install());State("ORIGINAL");});
    Case("Install on a separate full game copy",()=>{engine.Install();State("INSTALLED");foreach(var pair in Product.Entries.Select((e,i)=>new{e,i}))Check(Hash.FileHash(Backup(pair.i))==pair.e.Before,"Backup differs");});
    var installed=Product.Entries.Select((e,i)=>File.ReadAllBytes(Target(i))).ToArray();var backups=Product.Entries.Select((e,i)=>File.ReadAllBytes(Backup(i))).ToArray();
    Case("Repeated install is idempotent and preserves backups",()=>{engine.Install();State("INSTALLED");for(int i=0;i<backups.Length;i++)Check(Hash.Bytes(File.ReadAllBytes(Backup(i)))==Hash.Bytes(backups[i]),"Backup overwritten");});
    Case("Uninstall restores exact original files",()=>{engine.Restore();State("ORIGINAL");});
    Case("Reinstall and restore",()=>{engine.Install();State("INSTALLED");engine.Restore();State("ORIGINAL");});
    Case("Reject unknown game-file version without overwrite",()=>{var bytes=File.ReadAllBytes(Target(1));var bad=(byte[])bytes.Clone();bad[0]^=1;File.WriteAllBytes(Target(1),bad);Refused(()=>engine.Install());Check(Hash.FileHash(Target(1))==Hash.Bytes(bad),"Unknown file overwritten");File.WriteAllBytes(Target(1),bytes);State("ORIGINAL");});
    Case("Reject corrupted embedded payload before replacing game files",()=>{Refused(()=>new Engine(root,new CorruptPatch(payload),()=>false).Install());State("ORIGINAL");});
    Case("Failure after first replacement rolls back both files",()=>{Refused(()=>new Engine(root,payload,()=>false,i=>{if(i==0)throw new IOException("Injected disk failure after first replacement");}).Install());State("ORIGINAL");});
    Case("Recover known partial installation",()=>{File.WriteAllBytes(Target(0),installed[0]);File.WriteAllText(Path.Combine(root,Product.StateName,"state.txt"),"REPLACING");State("PARTIAL");engine.Restore();State("ORIGINAL");});
    Case("Reject damaged backup before uninstall changes",()=>{engine.Install();var bad=(byte[])backups[0].Clone();bad[0]^=1;File.WriteAllBytes(Backup(0),bad);Refused(()=>engine.Restore());State("INSTALLED");File.WriteAllBytes(Backup(0),backups[0]);engine.Restore();State("ORIGINAL");});
    Case("Reject missing original backup",()=>{engine.Install();File.Move(Backup(0),Backup(0)+".hold");Refused(()=>engine.Restore());State("INSTALLED");File.Move(Backup(0)+".hold",Backup(0));engine.Restore();State("ORIGINAL");});
    Case("Reject unknown installer state",()=>{var p=Path.Combine(root,Product.StateName,"identity.txt");var old=File.ReadAllText(p);File.WriteAllText(p,"UNKNOWN");Refused(()=>engine.Install());State("ORIGINAL");File.WriteAllText(p,old);});
    Case("Exclusive operation lock prevents concurrent writes",()=>{using(var f=new FileStream(Path.Combine(root,Product.StateName,"operation.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){Refused(()=>engine.Install());State("ORIGINAL");}});
    Case("Concurrent unknown edit is preserved and recovery is explicit",()=>{var foreign=(byte[])backups[1].Clone();foreign[0]^=1;Refused(()=>new Engine(root,payload,()=>false,i=>{if(i==0)File.WriteAllBytes(Target(1),foreign);}).Install());Check(Hash.FileHash(Target(1))==Hash.Bytes(foreign),"Concurrent foreign edit overwritten");Check(File.ReadAllText(Path.Combine(root,Product.StateName,"state.txt"))=="RECOVERY_REQUIRED","Missing explicit recovery status");File.WriteAllBytes(Target(1),backups[1]);engine.Restore();State("ORIGINAL");});
   }catch(Exception){code=1;}
   File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"results.json"),"{\"status\":\""+(code==0?"PASS":"FAIL")+"\",\"game_root\":"+Q(root)+",\"cases\":["+String.Join(",",results)+"]}",new UTF8Encoding(false));
   Console.WriteLine((code==0?"PASS":"FAIL")+": "+results.Count+" cases. No game process or installer GUI was launched.");return code;
  }
 }
}
