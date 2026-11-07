using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Web.Script.Serialization;using System.Security.Principal;using Microsoft.Win32;using System.Text;
using Marko22.PTTP;
class MemoryStore:ILanguageStore {
 public Dictionary<string,LanguageValue> values=new Dictionary<string,LanguageValue>();public string Identity{get{return "isolated-memory";}}public int writes,failAt;
 public LanguageValue Read(string n){return values.ContainsKey(n)?values[n]:new LanguageValue{Exists=false};}
 public void Write(string n,LanguageValue v){writes++;if(failAt==writes)throw new IOException("Injected registry write failure");if(v.Exists)values[n]=v;else values.Remove(n);}
 public void Flush(){}
}
class LanguageTests {
 static string output,root;static readonly List<object> results=new List<object>();
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Refused(Action a){bool blocked=false;try{a();}catch(Exception){blocked=true;}Check(blocked,"Unsafe language operation accepted");}
 static MemoryStore Initial(string code){var s=new MemoryStore();s.values[UserLanguage.Names[0]]=LanguageValue.Binary(code);s.values[UserLanguage.Names[1]]=LanguageValue.Binary(code=="pl"?"Polish":"English");s.values["unrelated-setting"]=LanguageValue.Binary("KEEP");return s;}
 static UserLanguage User(MemoryStore s,Func<bool> running=null){return new UserLanguage(root,s,"test-user",running??(()=>false));}
 static void PL(MemoryStore s){Check(s.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("pl"))&&s.Read(UserLanguage.Names[1]).Same(LanguageValue.Binary("Polish")),"Polish values not applied");Check(s.Read("unrelated-setting").Same(LanguageValue.Binary("KEEP")),"Unrelated preference touched");}
 static void Case(string name,Action a){root=Path.Combine(output,"case_"+(results.Count+1));Directory.CreateDirectory(root);try{a();results.Add(new{name=name,status="PASS"});}catch(Exception e){results.Add(new{name=name,status="FAIL",error=e.ToString()});}}
 static void Copy(string src,string dst){Directory.CreateDirectory(dst);foreach(var p in Directory.GetFiles(src))File.Copy(p,Path.Combine(dst,Path.GetFileName(p)));foreach(var p in Directory.GetDirectories(src))Copy(p,Path.Combine(dst,Path.GetFileName(p)));}
 static string Fingerprint(){using(var k=Registry.CurrentUser.OpenSubKey(RegistryLanguageStore.GameKey,false)){if(k==null)return "absent";var text=new StringBuilder();foreach(var name in k.GetValueNames().OrderBy(n=>n,StringComparer.Ordinal)){if(UserLanguage.Names.Contains(name))continue;var v=k.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames);text.Append(name).Append('|').Append(k.GetValueKind(name)).Append('|').Append(v is byte[]?Convert.ToBase64String((byte[])v):Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture)).Append('\n');}return Hash.Bytes(Encoding.UTF8.GetBytes(text.ToString()));}}
 static int Main(string[] args){
  output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
  if(args[0]=="--verify"){
   var previous=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(output,"real_profile_result.json")));Check(Fingerprint()==(string)previous["other_preferences_digest"],"Other production preferences changed during QA");
   var store=new RegistryLanguageStore(@"Software\PTTP_PL_LANGTEST_20261106\Power to the People");Check(store.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("pl"))&&store.Read(UserLanguage.Names[1]).Same(LanguageValue.Binary("Polish")),"Polish did not persist after two game exits");
   var real=new RegistryLanguageStore();Check(real.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("pl")),"Production profile no longer selects Polish");
   File.WriteAllText(Path.Combine(output,"persistence_after_restart.json"),new JavaScriptSerializer().Serialize(new{status="PASS",isolated_profile_still_polish=true,production_profile_polish=true,other_production_preferences_unchanged=true}));Console.WriteLine("PASS: Polish persisted after both exits; other production preferences unchanged.");return 0;
  }
  if(args[0]=="--real"){
   string game=Path.GetFullPath(args[2]),before=Fingerprint();var store=new RegistryLanguageStore();var old=UserLanguage.Names.Select(store.Read).ToArray();
   var prefs=new UserLanguage(Path.Combine(game,".marko22-pttp-pl-rc1"),()=>System.Diagnostics.Process.GetProcessesByName("Power to the People").Length>0);
   string msg=new Engine(game,new EmbeddedPatches(typeof(Engine).Assembly),preferences:prefs).Install();
   Check(UserLanguage.Names.Select(store.Read).Zip(new[]{LanguageValue.Binary("pl"),LanguageValue.Binary("Polish")},(a,b)=>a.Same(b)).All(x=>x),"Real Polish selection failed");Check(before==Fingerprint(),"Other real preferences changed");
   File.WriteAllText(Path.Combine(output,"real_profile_result.json"),new JavaScriptSerializer().Serialize(new{status="PASS",message=msg,previous=old,current=UserLanguage.Names.Select(store.Read).ToArray(),other_preferences_unchanged=true,other_preferences_digest=before,game_launched=false}));Console.WriteLine("PASS: real profile explicitly selects Polish; other preferences unchanged.");return 0;
  }
  if(args[0]=="--qa-enable"||args[0]=="--qa-restore"){
   string key=@"Software\PTTP_PL_LANGTEST_20261106\Power to the People",state=Path.Combine(output,"runtime_language_state");var store=new RegistryLanguageStore(key);var prefs=new UserLanguage(state,store,WindowsIdentity.GetCurrent().User.Value,()=>System.Diagnostics.Process.GetProcessesByName("Power to the People").Length>0);
   if(args[0]=="--qa-enable"){
    using(var k=Registry.CurrentUser.OpenSubKey(key,false))Check(k==null,"Isolated QA profile already exists; do not overwrite");
    store.Write(UserLanguage.Names[0],LanguageValue.Binary("en"));store.Write(UserLanguage.Names[1],LanguageValue.Binary("English"));store.Flush();
    Console.WriteLine(prefs.EnablePolish());
   }else{Console.WriteLine(prefs.RestorePrevious());Check(store.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("en")),"QA previous language not restored");}
   File.WriteAllText(Path.Combine(output,args[0]=="--qa-enable"?"qa_enable.json":"qa_restore.json"),new JavaScriptSerializer().Serialize(new{status="PASS",current=UserLanguage.Names.Select(store.Read).ToArray(),key=key}));return 0;
  }
  Case("English -> Polish; new process restores exact previous language",()=>{var s=Initial("en");User(s).EnablePolish();PL(s);User(s).RestorePrevious();Check(s.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("en")),"English not restored");});
  Case("Missing preferences -> create only two language values; restore absence",()=>{var s=new MemoryStore();User(s).EnablePolish();Check(s.values.Count==2,"Unexpected values created");User(s).RestorePrevious();Check(s.values.Count==0,"Absent values not removed");});
  Case("AUTO -> explicit Polish; restore exact AUTO choice",()=>{var s=Initial("");User(s).EnablePolish();PL(s);User(s).RestorePrevious();Check(s.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("")),"AUTO not restored");});
  Case("Repeated install preserves first language backup",()=>{var s=Initial("en");User(s).EnablePolish();User(s).EnablePolish();User(s).RestorePrevious();Check(s.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("en")),"Original choice overwritten with Polish");});
  Case("Existing Polish is preserved as previous language",()=>{var s=Initial("pl");User(s).EnablePolish();Check(s.writes==0,"Unnecessary registry writes");User(s).RestorePrevious();PL(s);});
  Case("Reinstall after removal starts a new backed-up cycle",()=>{var s=Initial("en");User(s).EnablePolish();User(s).RestorePrevious();s.values[UserLanguage.Names[0]]=LanguageValue.Binary("de");User(s).EnablePolish();User(s).RestorePrevious();Check(s.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("de")),"New cycle not preserved");});
  Case("Running game blocks install and restore language writes",()=>{var s=Initial("en");Refused(()=>User(s,()=>true).EnablePolish());Check(s.writes==0,"Running game language changed");User(s).EnablePolish();int writes=s.writes;Refused(()=>User(s,()=>true).RestorePrevious());Check(s.writes==writes,"Running game restore changed language");});
  Case("Interrupted write rolls back both values; resume retains original",()=>{var s=Initial("en");s.failAt=2;Refused(()=>User(s).EnablePolish());Check(s.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("en"))&&s.Read(UserLanguage.Names[1]).Same(LanguageValue.Binary("English")),"Partial preference write remained");User(s).EnablePolish();PL(s);User(s).RestorePrevious();Check(s.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("en")),"Original backup lost after retry");});
  Case("Damaged language backup blocks unverified preference change",()=>{var s=Initial("en");User(s).EnablePolish();File.WriteAllText(Directory.GetFiles(root,"language-*.json")[0],"{}");int writes=s.writes;Refused(()=>User(s).EnablePolish());Check(s.writes==writes,"Unknown snapshot accepted");});
  Case("Previous-release upgrade applies language only after file success, removal restores it",()=>{var s=Initial("en");string game=Path.Combine(root,"game");Copy(args[2],game);var prefs=new UserLanguage(Path.Combine(game,".marko22-pttp-pl-rc1"),s,"test-user",()=>false);var engine=new Engine(game,new EmbeddedPatches(typeof(Engine).Assembly),()=>false,preferences:prefs);Check(engine.Probe()=="UPGRADE","Legacy not recognized");engine.Install();Check(engine.Probe()=="INSTALLED","Upgrade not complete");PL(s);engine.Restore();Check(engine.Probe()=="ORIGINAL"&&s.Read(UserLanguage.Names[0]).Same(LanguageValue.Binary("en")),"Removal did not restore original game/language");});
  Case("File installation failure cannot select Polish",()=>{var s=Initial("en");string game=Path.Combine(root,"game");Copy(args[2],game);var prefs=new UserLanguage(Path.Combine(game,".marko22-pttp-pl-rc1"),s,"test-user",()=>false);Refused(()=>new Engine(game,new EmbeddedPatches(typeof(Engine).Assembly),()=>false,i=>{throw new IOException("Injected write failure");},prefs).Install());Check(s.writes==0,"Language applied before successful installation");});
  Case("Game appearing after installation defers language without unsafe writes",()=>{var s=Initial("en");string game=Path.Combine(root,"game");Copy(args[2],game);var prefs=new UserLanguage(Path.Combine(game,".marko22-pttp-pl-rc1"),s,"test-user",()=>true);var engine=new Engine(game,new EmbeddedPatches(typeof(Engine).Assembly),()=>false,preferences:prefs);string msg=engine.Install();Check(engine.Probe()=="INSTALLED"&&msg.Contains("Nie ustawiono")&&s.writes==0,"Late-running game was not safely deferred");});
  bool ok=!results.Any(x=>new JavaScriptSerializer().Serialize(x).Contains("\"FAIL\""));File.WriteAllText(Path.Combine(output,"tests.json"),new JavaScriptSerializer().Serialize(new{status=ok?"PASS":"FAIL",cases=results}));Console.WriteLine((ok?"PASS":"FAIL")+": "+results.Count+" language-focused cases; no real preferences changed.");return ok?0:1;
 }
}
