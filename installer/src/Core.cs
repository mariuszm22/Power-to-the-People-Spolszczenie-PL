using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;
namespace Marko22.PTTP {
 public sealed class Entry {
  public readonly string Path,Before,After,PatchHash,Resource;public readonly int OldSize,NewSize;
  public Entry(string p,string b,string a,string h,string r,int oldSize,int newSize){Path=p;Before=b;After=a;PatchHash=h;Resource=r;OldSize=oldSize;NewSize=newSize;}
 }
 public interface IPatches {byte[] Get(string name);}
 public sealed class EmbeddedPatches:IPatches {
  readonly Assembly assembly;public EmbeddedPatches(Assembly a){assembly=a;}
  public byte[] Get(string n){using(var s=assembly.GetManifestResourceStream("PTTP."+n)){if(s==null)throw new IOException("Brak wbudowanej łatki.");using(var m=new MemoryStream()){s.CopyTo(m);return m.ToArray();}}}
 }
 public static class Hash {
  public static string Bytes(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
  public static string FileHash(string f){using(var s=File.OpenRead(f))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 }
 public sealed class Engine {
  public readonly string Root;readonly string state;readonly IPatches patches;readonly Func<bool> running;readonly Action<int> afterReplace;readonly IUserLanguage language;
  const string Magic="marko22/PTTP/1413370/0.9.0-rc1";
  public Engine(string root,IPatches source,Func<bool> runningCheck=null,Action<int> fault=null,IUserLanguage preferences=null){Root=System.IO.Path.GetFullPath(root).TrimEnd('\\','/');state=System.IO.Path.Combine(Root,Product.StateName);patches=source;running=runningCheck??(()=>Process.GetProcessesByName("Power to the People").Length>0);afterReplace=fault;language=preferences??new NoLanguageChanges();}
  string InstalledLanguage(){try{string message=language.EnablePolish();return String.IsNullOrEmpty(message)?"Zainstalowano język polski. Wybierz Polski w ustawieniach gry.":message;}catch(Exception e){return "Pliki spolszczenia zainstalowano. Nie ustawiono języka automatycznie: "+e.Message;}}
  string RestoredLanguage(){try{return language.RestorePrevious();}catch(Exception e){return "Pliki gry przywrócono. Przywrócenie języka wymaga ponowienia po zamknięciu gry: "+e.Message;}}
  string Target(Entry e){return Safe(e.Path);}string Backup(int i){return System.IO.Path.Combine(state,"original-"+i+".bin");}
  string Safe(string relative){var p=System.IO.Path.GetFullPath(System.IO.Path.Combine(Root,relative));if(!p.StartsWith(Root+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Ścieżka wykracza poza grę.");return p;}
  void NotLink(string p){if((Directory.Exists(p)||File.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new IOException("Nieobsługiwane dowiązanie/junction: "+p);}
  void Guard(){
   if(!Directory.Exists(Root))throw new IOException("Folder nie istnieje.");
   NotLink(Root);NotLink(Safe("Power to the People_Data"));NotLink(Safe("Power to the People_Data\\Managed"));NotLink(state);
   var exe=Safe("Power to the People.exe");NotLink(exe);
   if(!File.Exists(exe)||Hash.FileHash(exe)!=Product.ExeHash)throw new IOException("Niewłaściwa gra lub nieobsługiwana wersja pliku EXE. Wybierz folder Power to the People, build 20302782.");
   foreach(var e in Product.Entries){NotLink(Target(e));if(!File.Exists(Target(e)))throw new IOException("Brak pliku: "+e.Path);}
  }
  string ReadState(out string[] hashes){Guard();int before=0,after=0;hashes=new string[Product.Entries.Length];for(int i=0;i<Product.Entries.Length;i++){var e=Product.Entries[i];var h=hashes[i]=Hash.FileHash(Target(e));if(h==e.Before)before++;else if(h==e.After)after++;else if(!KnownReleases.Contains(i,h))throw new IOException("Nieznana wersja pliku — niczego nie nadpisano: "+e.Path);}
   return before==Product.Entries.Length?"ORIGINAL":after==Product.Entries.Length?"INSTALLED":KnownReleases.Matches(hashes)?"UPGRADE":"PARTIAL";
  }
  public string Probe(){string[] hashes;return ReadState(out hashes);}
  void EnsureState(){
   if(Directory.Exists(state)){NotLink(state);var id=System.IO.Path.Combine(state,"identity.txt");NotLink(id);if(!File.Exists(id)||File.ReadAllText(id)!=Magic)throw new IOException("Nieznany folder stanu instalacji. Zatrzymano zapis.");}
   else {Directory.CreateDirectory(state);File.WriteAllText(System.IO.Path.Combine(state,"identity.txt"),Magic);}
   foreach(var p in Directory.GetFiles(state)){NotLink(p);}
  }
  FileStream Lock(){return new FileStream(System.IO.Path.Combine(state,"operation.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
  void Journal(string value){var p=System.IO.Path.Combine(state,"state.txt");var tmp=p+".new";File.WriteAllText(tmp,value,new UTF8Encoding(false));using(var f=new FileStream(tmp,FileMode.Open,FileAccess.Write,FileShare.None)){f.Flush(true);}if(File.Exists(p))File.Replace(tmp,p,null);else File.Move(tmp,p);}
  void Backups(bool create){
   for(int i=0;i<Product.Entries.Length;i++){var e=Product.Entries[i];var b=Backup(i);NotLink(b);
    if(File.Exists(b)){if(Hash.FileHash(b)!=e.Before)throw new IOException("Uszkodzona/nieznana kopia bezpieczeństwa "+i+". Zatrzymano bez podmiany.");}
    else {if(!create)throw new IOException("Brak oryginalnego backupu. Nie można bezpiecznie przywrócić gry.");if(Hash.FileHash(Target(e))!=e.Before)throw new IOException("Nie można utworzyć oryginalnego backupu z pliku zmienionego.");File.Copy(Target(e),b,false);if(Hash.FileHash(b)!=e.Before)throw new IOException("Błąd zapisu backupu.");}
   }
  }
  byte[] Apply(Entry e,byte[] original){
   var compressed=patches.Get(e.Resource);if(Hash.Bytes(compressed)!=e.PatchHash)throw new IOException("Niepoprawna suma wbudowanej łatki.");
   using(var g=new GZipStream(new MemoryStream(compressed),CompressionMode.Decompress))using(var r=new BinaryReader(g))using(var o=new MemoryStream()){
    if(Encoding.ASCII.GetString(r.ReadBytes(8))!="PTTPPLP1"||r.ReadUInt32()!=e.OldSize||r.ReadUInt32()!=e.NewSize)throw new IOException("Niepoprawny nagłówek łatki.");
    var n=r.ReadUInt32();if(n>500000)throw new IOException("Zbyt wiele operacji łatki.");
    for(uint i=0;i<n;i++){var type=r.ReadByte();if(type==0){var off=r.ReadUInt32();var count=r.ReadUInt32();if((long)off+count>original.Length||(long)o.Length+count>e.NewSize)throw new IOException("Nieprawidłowy zakres COPY.");o.Write(original,(int)off,(int)count);}
     else if(type==1){var count=r.ReadUInt32();if((long)o.Length+count>e.NewSize)throw new IOException("Nieprawidłowy zakres DATA.");var b=r.ReadBytes((int)count);if(b.Length!=count)throw new IOException("Niepełna łatka.");o.Write(b,0,b.Length);}else throw new IOException("Nieznana operacja łatki.");}
    if(o.Length!=e.NewSize||r.Read()!=-1)throw new IOException("Niewłaściwy rozmiar lub nadmiar danych łatki.");var result=o.ToArray();if(Hash.Bytes(result)!=e.After)throw new IOException("Wynik łatki ma niewłaściwy SHA-256.");return result;
   }
  }
  string Stage(int i,byte[] data){var p=System.IO.Path.Combine(state,"stage-"+i+".tmp");NotLink(p);File.WriteAllBytes(p,data);using(var f=new FileStream(p,FileMode.Open,FileAccess.Write,FileShare.None)){f.Flush(true);}if(Hash.FileHash(p)!=Hash.Bytes(data))throw new IOException("Błąd pliku tymczasowego.");return p;}
  void RestoreKnown(){
   Backups(false);
   for(int i=0;i<Product.Entries.Length;i++){var e=Product.Entries[i];var h=Hash.FileHash(Target(e));if(h!=e.Before&&h!=e.After&&!KnownReleases.Contains(i,h))throw new IOException("Plik zmieniono po instalacji. Automatyczne przywrócenie wstrzymane: "+e.Path);}
   for(int i=0;i<Product.Entries.Length;i++){var e=Product.Entries[i];var h=Hash.FileHash(Target(e));if(h==e.Before)continue;if(h!=e.After&&!KnownReleases.Contains(i,h))throw new IOException("Plik zmienił się podczas przywracania; nie został nadpisany.");var p=Stage(i,File.ReadAllBytes(Backup(i)));if(Hash.FileHash(p)!=e.Before||Hash.FileHash(Target(e))!=h)throw new IOException("Plik lub backup zmienił się podczas przywracania.");File.Replace(p,Target(e),null);}
   foreach(var e in Product.Entries)if(Hash.FileHash(Target(e))!=e.Before)throw new IOException("Niepełne przywrócenie.");Journal("REMOVED");
  }
  public string Install(){
   if(running())throw new IOException("Zamknij grę przed instalacją.");var status=Probe();if(status=="PARTIAL")throw new IOException("Wykryto częściową instalację. Wybierz Przywróć.");
   if(status=="UPGRADE"&&!Directory.Exists(state))throw new IOException("Brak stanu i oryginalnego backupu poprzedniej wersji. Aktualizacja wstrzymana.");
   EnsureState();using(Lock()){
    string[] sourceHashes;if(ReadState(out sourceHashes)!=status)throw new IOException("Stan plików zmienił się przed operacją. Niczego nie nadpisano.");
    if(status=="INSTALLED"){Backups(false);return InstalledLanguage();}
    Backups(status!="UPGRADE");var staged=new string[Product.Entries.Length];bool changing=false;
    try{
     for(int i=0;i<Product.Entries.Length;i++){var e=Product.Entries[i];if(sourceHashes[i]==e.After)continue;staged[i]=Stage(i,Apply(e,File.ReadAllBytes(status=="UPGRADE"?Backup(i):Target(e))));}
     // Recheck all source hashes after staging and before the first replacement.
     for(int i=0;i<Product.Entries.Length;i++)if(Hash.FileHash(Target(Product.Entries[i]))!=sourceHashes[i])throw new IOException("Wersja źródłowa zmieniła się podczas przygotowania.");
     if(running())throw new IOException("Gra została uruchomiona podczas przygotowania. Zapis wstrzymano.");
     Journal("PREPARED");changing=true;Journal("REPLACING");
     for(int i=0;i<Product.Entries.Length;i++){if(Hash.FileHash(Target(Product.Entries[i]))!=sourceHashes[i])throw new IOException("Plik zmienił się tuż przed podmianą; nie został nadpisany.");if(staged[i]==null)continue;File.Replace(staged[i],Target(Product.Entries[i]),null);if(afterReplace!=null)afterReplace(i);}
     foreach(var e in Product.Entries)if(Hash.FileHash(Target(e))!=e.After)throw new IOException("Niepełna instalacja.");Journal("INSTALLED");return InstalledLanguage();
    }catch(Exception error){if(changing){try{RestoreKnown();}catch(Exception rollback){Journal("RECOVERY_REQUIRED");throw new IOException("Instalacja przerwana i wymaga ręcznej kontroli; zachowano backupy. "+error.Message+" / "+rollback.Message,error);}}throw;}
   }
  }
  public string Restore(){if(running())throw new IOException("Zamknij grę przed przywróceniem.");var status=Probe();if(status=="ORIGINAL"&&!Directory.Exists(state))return "Pliki gry są oryginalne; spolszczenie nie jest zainstalowane.";EnsureState();using(Lock()){RestoreKnown();return "Przywrócono oryginalne pliki gry. "+RestoredLanguage();}}
 }
 public static class Steam {
  public static List<string> Detect(){var found=new List<string>();var roots=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var key in new[]{Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"),Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")}){if(key==null)continue;using(key){var p=key.GetValue("SteamPath") as string??key.GetValue("InstallPath") as string;if(!String.IsNullOrEmpty(p))roots.Add(p);}}
   foreach(var p in roots.ToArray()){var vdf=System.IO.Path.Combine(p,"steamapps","libraryfolders.vdf");if(File.Exists(vdf)&&new FileInfo(vdf).Length<2000000){foreach(Match m in Regex.Matches(File.ReadAllText(vdf),"\"path\"\\s*\"([^\"]+)\""))roots.Add(m.Groups[1].Value.Replace("\\\\","\\"));}}
   foreach(var p in roots){var game=System.IO.Path.Combine(p,"steamapps","common","Power to the People");if(File.Exists(System.IO.Path.Combine(game,"Power to the People.exe")))found.Add(game);}return found;
  }
 }
}
