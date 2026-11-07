using System;using System.IO;using System.Text;using System.Runtime.Serialization;using System.Runtime.Serialization.Json;using System.Security.Principal;using Microsoft.Win32;
namespace Marko22.PTTP {
 public interface IUserLanguage {string EnablePolish();string RestorePrevious();}
 internal sealed class NoLanguageChanges:IUserLanguage {public string EnablePolish(){return "";}public string RestorePrevious(){return "";}}
 [DataContract]public sealed class LanguageValue {
  [DataMember]public bool Exists;[DataMember]public string Data;
  public bool Same(LanguageValue other){return Exists==other.Exists&&(!Exists||String.Equals(Data,other.Data,StringComparison.Ordinal));}
  public static LanguageValue Binary(string value){return new LanguageValue{Exists=true,Data=Convert.ToBase64String(Encoding.UTF8.GetBytes(value+"\0"))};}
 }
 public interface ILanguageStore {string Identity{get;}LanguageValue Read(string name);void Write(string name,LanguageValue value);void Flush();}
 public sealed class RegistryLanguageStore:ILanguageStore {
  public const string GameKey=@"Software\HermesInteractive\Power to the People";readonly string path;
  public RegistryLanguageStore():this(GameKey){}public RegistryLanguageStore(string keyPath){path=keyPath;}
  public string Identity{get{return path;}}
  public LanguageValue Read(string name){using(var key=Registry.CurrentUser.OpenSubKey(path,false)){if(key==null||Array.IndexOf(key.GetValueNames(),name)<0)return new LanguageValue{Exists=false};if(key.GetValueKind(name)!=RegistryValueKind.Binary)throw new IOException("Nietypowy format ustawienia języka. Pozostawiono je bez zmian.");var bytes=(byte[])key.GetValue(name);if(bytes.Length>512)throw new IOException("Nietypowa długość ustawienia języka.");return new LanguageValue{Exists=true,Data=Convert.ToBase64String(bytes)};}}
  public void Write(string name,LanguageValue value){using(var key=Registry.CurrentUser.CreateSubKey(path)){if(value.Exists)key.SetValue(name,Convert.FromBase64String(value.Data),RegistryValueKind.Binary);else key.DeleteValue(name,false);}}
  public void Flush(){using(var key=Registry.CurrentUser.OpenSubKey(path,true)){if(key!=null)key.Flush();}}
 }
 [DataContract]internal sealed class LanguageSnapshot {
  [DataMember]public int Schema;[DataMember]public string Owner,State;[DataMember]public LanguageValue[] Original;
 }
 public sealed class UserLanguage:IUserLanguage {
  public static readonly string[] Names={"forcedLanguage_h129151182","I2 Language_h3293684300"};
  readonly string directory,file,owner;readonly ILanguageStore store;readonly Func<bool> running;
  static readonly LanguageValue[] Polish={LanguageValue.Binary("pl"),LanguageValue.Binary("Polish")};
  public UserLanguage(string state,Func<bool> runningCheck):this(state,new RegistryLanguageStore(),WindowsIdentity.GetCurrent().User.Value,runningCheck){}
  public UserLanguage(string state,ILanguageStore preferences,string userIdentity,Func<bool> runningCheck){directory=Path.GetFullPath(state);store=preferences;running=runningCheck;owner=Hash.Bytes(Encoding.UTF8.GetBytes(userIdentity+"|"+store.Identity));file=Path.Combine(directory,"language-"+owner+".json");}
  void Closed(){if(running())throw new IOException("Gra jest uruchomiona. Zamknij ją i ponów instalację, aby bezpiecznie zapisać język.");}
  static void NotLink(string path){if((File.Exists(path)||Directory.Exists(path))&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new IOException("Nieobsługiwane dowiązanie w kopii ustawienia języka.");}
  LanguageValue[] Current(){var values=new LanguageValue[Names.Length];for(int i=0;i<Names.Length;i++)values[i]=store.Read(Names[i]);return values;}
  void Write(LanguageValue[] values){for(int i=0;i<Names.Length;i++)if(!store.Read(Names[i]).Same(values[i]))store.Write(Names[i],values[i]);store.Flush();for(int i=0;i<Names.Length;i++)if(!store.Read(Names[i]).Same(values[i]))throw new IOException("Weryfikacja zapisu języka nie powiodła się.");}
  LanguageSnapshot Load(){
   NotLink(directory);NotLink(file);if(!File.Exists(file))return null;if(new FileInfo(file).Length>8192)throw new IOException("Niepoprawna kopia języka.");
   LanguageSnapshot s;using(var f=File.OpenRead(file))s=(LanguageSnapshot)new DataContractJsonSerializer(typeof(LanguageSnapshot)).ReadObject(f);
   if(s==null||s.Schema!=1||s.Owner!=owner||s.Original==null||s.Original.Length!=2||(s.State!="pending"&&s.State!="applied"&&s.State!="restored"))throw new IOException("Kopia języka nie pasuje do bieżącego profilu.");
   foreach(var v in s.Original)if(v==null||(v.Exists&&(v.Data==null||Convert.FromBase64String(v.Data).Length>512)))throw new IOException("Niepoprawny zapis poprzedniego języka.");return s;
  }
  void Save(LanguageSnapshot snapshot){
   NotLink(directory);Directory.CreateDirectory(directory);NotLink(file);string temp=file+".new";NotLink(temp);
   using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){new DataContractJsonSerializer(typeof(LanguageSnapshot)).WriteObject(f,snapshot);f.Flush(true);}
   if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);
  }
  public string EnablePolish(){
   Closed();var snapshot=Load();var current=Current();
   if(snapshot!=null&&snapshot.State=="pending")for(int i=0;i<2;i++)if(!current[i].Same(snapshot.Original[i])&&!current[i].Same(Polish[i]))throw new IOException("Ustawienie języka zmieniło się po przerwanej operacji. Nie nadpisano go.");
   if(snapshot==null||snapshot.State=="restored"){
    if(snapshot!=null)File.Copy(file,file+".history-"+Guid.NewGuid().ToString("N"),false);
    snapshot=new LanguageSnapshot{Schema=1,Owner=owner,State="pending",Original=current};
   }
   snapshot.State="pending";Save(snapshot);
   try{Closed();Write(Polish);snapshot.State="applied";Save(snapshot);return "Automatycznie ustawiono język Polski. Uruchom grę samodzielnie.";}
   catch(Exception error){try{if(!running())Write(current);}catch(Exception rollback){throw new IOException("Nie udało się zakończyć zapisu języka; zachowano jego kopię. "+error.Message+" / "+rollback.Message,error);}throw;}
  }
  public string RestorePrevious(){
   Closed();var snapshot=Load();if(snapshot==null||snapshot.State=="restored")return "Brak aktywnej zmiany języka tego instalatora.";
   var current=Current();try{Closed();Write(snapshot.Original);snapshot.State="restored";Save(snapshot);return "Przywrócono poprzednie ustawienie języka.";}
   catch{try{if(!running())Write(current);}catch{}throw;}
  }
 }
}
