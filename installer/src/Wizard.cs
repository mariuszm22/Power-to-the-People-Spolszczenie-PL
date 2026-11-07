using System;using System.IO;using System.Drawing;using System.Windows.Forms;using System.ComponentModel;using System.Reflection;using System.Diagnostics;
namespace Marko22.PTTP {
 internal static class Program {
  [STAThread]static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Wizard());}
 }
 // Presentation revision of the existing Wizard. Engine and backup logic are unchanged.
 public sealed class Wizard:Form {
  int step;bool busy,lastSuccess,lastRestore;string operationMessage="",pathStatus="",validationMessage="";Engine engine;
  readonly BufferedContentPanel content=new BufferedContentPanel();readonly TranslucentPanel shell=new TranslucentPanel();
  readonly RoundedButton next=new RoundedButton(),back=new RoundedButton(),cancel=new RoundedButton(),website=new RoundedButton(),support=new RoundedButton(),reportButton=new RoundedButton();
  readonly FlowLayoutPanel projectLinks=new FlowLayoutPanel();readonly TextBox folder=new TextBox();readonly ComboBox mode=new ComboBox();
  readonly string[] headings={"Power to the People — Spolszczenie","Informacje przed instalacją","Wybór i weryfikacja gry","Gotowe do instalacji","Instalowanie spolszczenia","Instalacja zakończona"};
  const string WebsiteUrl="https://marko22.pl",Support="https://paypal.me/mariuszm22";
  const string ReportFile="Kulisy_spolszczenia_Power_to_the_People.html";
  Action<string> linkLauncher=LaunchWithShell;
  static readonly Color Amber=Color.FromArgb(230,180,81),Muted=Color.FromArgb(190,200,207),Green=Color.FromArgb(125,211,153),Red=Color.FromArgb(235,137,117);
  public Wizard(){
   SuspendLayout();AutoScaleDimensions=new SizeF(96F,96F);AutoScaleMode=AutoScaleMode.Dpi;
   Text="Instalator spolszczenia marko22 — Power to the People";ClientSize=new Size(1080,750);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;MinimizeBox=true;StartPosition=FormStartPosition.CenterScreen;
   Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(8,11,14);ForeColor=Color.WhiteSmoke;Icon=Icon.ExtractAssociatedIcon(typeof(Wizard).Assembly.Location);
   // Keep the original 1080 x 700 artwork and navigation; links get their own footer.
   var scene=new LayeredAssetHost{Dock=DockStyle.Top,Height=700};scene.LoadEmbeddedProfile();Controls.Add(scene);scene.SetBounds(0,0,1080,700);
   shell.SetBounds(428,32,620,636);shell.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Right;shell.Padding=new Padding(34,28,34,24);scene.Controls.Add(shell);
   var nav=new TableLayoutPanel{Name="Navigation",Dock=DockStyle.Bottom,Height=52,ColumnCount=4,RowCount=1,Margin=new Padding(0),Padding=new Padding(0,8,0,0),BackColor=Color.Transparent};
   nav.RowStyles.Add(new RowStyle(SizeType.Percent,100));foreach(float width in new float[]{18,18,29,35})nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,width));
   Button(back,"WSTECZ",false);Button(cancel,"ANULUJ",false);Button(next,"DALEJ",true);
   foreach(var secondary in new[]{back,cancel}){secondary.SetPalette(Color.FromArgb(52,68,86),Color.FromArgb(64,85,107),Color.FromArgb(40,54,70),Color.FromArgb(41,55,70),Color.White,Color.FromArgb(164,175,188));secondary.BorderColor=Color.FromArgb(96,117,138);secondary.Font=next.Font;}
   back.Dock=cancel.Dock=next.Dock=DockStyle.Fill;back.Margin=cancel.Margin=new Padding(0,0,6,0);next.Margin=new Padding(0);
   back.Click+=(s,e)=>{if(busy)return;if(step==5)step=2;else if(step>0)step--;Render();};cancel.Click+=(s,e)=>Close();next.Click+=Next;
   nav.Controls.Add(back,0,0);nav.Controls.Add(cancel,1,0);nav.Controls.Add(next,3,0);
   Button(website,"marko22.pl",false);Button(reportButton,"Kulisy spolszczenia",false);Button(support,"Wesprzyj projekt",false);
   foreach(var link in new[]{website,reportButton,support})link.SetPalette(Color.FromArgb(30,92,58),Color.FromArgb(41,115,73),Color.FromArgb(22,72,45),Color.FromArgb(30,52,39),Color.FromArgb(239,255,244),Color.FromArgb(140,168,150));
   website.Size=reportButton.Size=support.Size=new Size(180,34);
   website.Margin=reportButton.Margin=new Padding(0,0,16,0);support.Margin=new Padding(0);
   website.Click+=(s,e)=>OpenLink(WebsiteUrl);reportButton.Click+=(s,e)=>OpenReport();support.Click+=(s,e)=>OpenLink(Support);
   projectLinks.Name="ProjectLinks";projectLinks.BackColor=Color.Transparent;projectLinks.Size=new Size(572,34);projectLinks.WrapContents=false;projectLinks.AutoScroll=false;projectLinks.Margin=new Padding(0);projectLinks.Padding=new Padding(0);
   projectLinks.Controls.Add(website);projectLinks.Controls.Add(reportButton);projectLinks.Controls.Add(support);
   var footer=new Panel{Name="ProjectFooter",Dock=DockStyle.Bottom,Height=50,BackColor=Color.FromArgb(8,11,14)};
   footer.Controls.Add(projectLinks);Controls.Add(footer);
   footer.Layout+=(s,e)=>{projectLinks.Left=(footer.ClientSize.Width-projectLinks.Width)/2;projectLinks.Top=(footer.ClientSize.Height-projectLinks.Height)/2;};
   var originalLinkSpace=new Panel{Dock=DockStyle.Bottom,Height=42,BackColor=Color.Transparent};
   content.Dock=DockStyle.Fill;content.BackColor=Color.Transparent;shell.Controls.Add(content);shell.Controls.Add(nav);shell.Controls.Add(originalLinkSpace);
   folder.BackColor=Color.FromArgb(12,16,19);folder.ForeColor=Color.WhiteSmoke;folder.BorderStyle=BorderStyle.FixedSingle;folder.Font=new Font("Segoe UI",9);
   mode.DropDownStyle=ComboBoxStyle.DropDownList;mode.FlatStyle=FlatStyle.Flat;mode.BackColor=Color.FromArgb(12,16,19);mode.ForeColor=Color.WhiteSmoke;mode.Items.AddRange(new object[]{"Zainstaluj spolszczenie","Usuń / przywróć oryginały"});mode.SelectedIndex=0;
   folder.TextChanged+=(s,e)=>{validationMessage="Ścieżka zmieniona. Zgodność zostanie sprawdzona przed przejściem dalej.";pathStatus="";};
   FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};
   try{var found=Steam.Detect();if(found.Count>0)folder.Text=found[0];}catch{validationMessage="Automatyczne wykrywanie niedostępne. Wskaż folder ręcznie.";}
   ResumeLayout(false);PerformAutoScale();PerformLayout();Render();
  }
  int Units(int value){return (int)Math.Round(value*CurrentAutoScaleDimensions.Width/96F);}
  int PageWidth{get{return Math.Max(Units(100),content.ClientSize.Width-Units(12));}}
  static void Button(RoundedButton b,string text,bool primary){b.Text=text;b.Font=new Font("Segoe UI",primary?9:8,FontStyle.Bold);if(primary)b.SetPalette(Color.FromArgb(205,151,47),Color.FromArgb(218,165,60),Color.FromArgb(184,132,36),Color.FromArgb(111,88,47),Color.FromArgb(13,16,18),Color.FromArgb(65,59,48));}
  Label Label(string text,float size,FontStyle style,Color color,int bottom=10){
   var font=new Font("Segoe UI",size,style);int width=PageWidth,h;
   using(var g=CreateGraphics())h=TextRenderer.MeasureText(g,text,font,new Size(width,Int32.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl).Height+Units(4);
   return new Label{Text=text,Width=width,Height=h,Font=font,ForeColor=color,BackColor=Color.Transparent,Margin=new Padding(0,0,0,Units(bottom)),UseMnemonic=false};
  }
  void Card(FlowLayoutPanel page,string name,string value){var k=Label(name,9,FontStyle.Bold,Muted,0);var v=Label(value,11,FontStyle.Bold,Color.WhiteSmoke,0);v.Top=Math.Max(Units(25),k.Bottom+Units(3));var card=new Panel{Width=PageWidth,Height=Math.Max(Units(69),v.Bottom),BackColor=Color.Transparent,Margin=new Padding(0,0,0,Units(15))};card.Controls.Add(k);card.Controls.Add(v);page.Controls.Add(card);}
  void Render(){
   if(folder.Parent!=null)folder.Parent.Controls.Remove(folder);if(mode.Parent!=null)mode.Parent.Controls.Remove(mode);
   while(content.Controls.Count>0)content.Controls[0].Dispose();
   var page=new FlowLayoutPanel{Name="WizardPage",Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=false,Margin=new Padding(0),Padding=new Padding(0),BackColor=Color.Transparent};content.Controls.Add(page);
   page.Controls.Add(Label("KROK "+(step+1)+" Z 6",9,FontStyle.Bold,Amber,8));
   string heading=headings[step];if(step==3&&mode.SelectedIndex==1)heading="Gotowe do przywrócenia";if(step==4&&mode.SelectedIndex==1)heading="Przywracanie oryginałów";if(step==5)heading=lastSuccess?(lastRestore?"Przywracanie zakończone":"Instalacja zakończona"):"Operacja wstrzymana";
   page.Controls.Add(Label(heading,step==0?20:24,FontStyle.Bold,Color.White,14));
   back.Visible=step>0&&step<4||step==5&&!lastSuccess;back.Enabled=!busy;cancel.Visible=step<4;cancel.Enabled=!busy;next.Visible=step!=4;next.Enabled=!busy;
   next.Text=step==1?"ROZUMIEM I KONTYNUUJ":step==3?(mode.SelectedIndex==0?"ZAINSTALUJ":"PRZYWRÓĆ"):step==5?"ZAKOŃCZ":"DALEJ";
   projectLinks.Visible=true;projectLinks.Enabled=true;
   if(step==0){
    page.Controls.Add(Label("Kreator przeprowadzi Cię przez instalację polskiej lokalizacji.",11,FontStyle.Regular,Muted,23));
    Card(page,"WERSJA SPOLSZCZENIA",Product.Version+" · spolszczenie marko22");Card(page,"WYKRYWANIE GRY",String.IsNullOrWhiteSpace(folder.Text)?"Wybór automatyczny lub ręczny":"Znaleziono lokalną instalację Steam");
    page.Controls.Add(Label("Nieoficjalna polska lokalizacja autorstwa marko22.",10,FontStyle.Regular,Muted,16));page.Controls.Add(Label("Kliknij „Dalej”, aby zapoznać się z informacjami przed instalacją.",10,FontStyle.Regular,Muted));
   }
   if(step==1)page.Controls.Add(Label("• Polska lokalizacja obejmuje 1677 rekordów. Wersja 1.0.0 zachowuje pozostałe języki gry.\n\n• Wymagana jest własna gra Steam, build 20302782. Kreator sprawdzi pliki przed zapisem.\n\n• Zamknij grę. Instalator nie uruchamia gry i nie zmienia zapisów ani ustawień Steam.\n\n• Oryginalne pliki zostaną zachowane w kopii bezpieczeństwa. Ten sam program umożliwia usunięcie spolszczenia.\n\n• Nieznane lub zmienione wersje plików są odrzucane. Aktualizacja gry może wymagać nowego pakietu.",10,FontStyle.Regular,Color.WhiteSmoke));
   if(step==2){
    page.Controls.Add(Label("Wybierz folder z Power to the People.exe. Kreator sprawdzi wersję przed przejściem dalej.",11,FontStyle.Regular,Muted,17));page.Controls.Add(Label("KATALOG GRY",9,FontStyle.Bold,Muted,5));
    var row=new TableLayoutPanel{Width=PageWidth,Height=Units(38),ColumnCount=2,Margin=new Padding(0,0,0,Units(15))};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,Units(130)));folder.Dock=DockStyle.Fill;folder.Margin=new Padding(0,Units(2),Units(7),0);var browse=new RoundedButton{Text="WYBIERZ RĘCZNIE",Dock=DockStyle.Fill,Font=new Font("Segoe UI",8,FontStyle.Bold)};
    browse.Click+=(s,e)=>{using(var d=new FolderBrowserDialog{Description="Wskaż katalog Power to the People",ShowNewFolderButton=false}){if(Directory.Exists(folder.Text))d.SelectedPath=folder.Text;if(d.ShowDialog(this)==DialogResult.OK){folder.Text=d.SelectedPath;InspectPath();Render();}}};row.Controls.Add(folder,0,0);row.Controls.Add(browse,1,0);page.Controls.Add(row);
    var operations=new FlowLayoutPanel{Width=PageWidth,Height=Units(40),WrapContents=false,BackColor=Color.Transparent,Margin=new Padding(0,0,0,Units(18))};
    int choiceWidth=(PageWidth-Units(8))/2;RoundedButton installChoice=new RoundedButton{Width=choiceWidth,Height=Units(34),Margin=new Padding(0,0,Units(8),0)},restoreChoice=new RoundedButton{Width=choiceWidth,Height=Units(34),Margin=new Padding(0)};
    Button(installChoice,"INSTALUJ SPOLSZCZENIE",mode.SelectedIndex==0);Button(restoreChoice,"USUŃ / PRZYWRÓĆ",mode.SelectedIndex==1);
    installChoice.Click+=(s,e)=>{mode.SelectedIndex=0;InspectPath();Render();};restoreChoice.Click+=(s,e)=>{mode.SelectedIndex=1;InspectPath();Render();};operations.Controls.Add(installChoice);operations.Controls.Add(restoreChoice);page.Controls.Add(operations);
    page.Controls.Add(Label(String.IsNullOrEmpty(validationMessage)?"Build wymagany: 20302782. Pliki zostaną sprawdzone sumami SHA-256.\nŻaden plik nie zostanie zmieniony na tym ekranie.":validationMessage,10,FontStyle.Regular,pathStatus==""?Amber:Green));
   }
   if(step==3){
    Card(page,"GRA / WERSJA","Power to the People · Steam 20302782");page.Controls.Add(Label("Operacja: "+(mode.SelectedIndex==0?"instalacja języka polskiego":"usunięcie / przywrócenie oryginałów"),11,FontStyle.Bold,Color.WhiteSmoke,13));
    page.Controls.Add(Label("Pliki do zmiany:\n• Power to the People_Data\\resources.assets\n• Power to the People_Data\\Managed\\Surge.Main.dll",9.5F,FontStyle.Regular,Muted,14));page.Controls.Add(Label("Backup: "+Product.StateName+"\nNieznane pliki i backupy nie zostaną nadpisane. Podczas zapisu poczekaj na zakończenie operacji.",9.5F,FontStyle.Regular,Muted,14));page.Controls.Add(Label(folder.Text,8.5F,FontStyle.Regular,Muted));
   }
   if(step==4){page.Controls.Add(Label("Trwa kontrola i zapis plików. Nie zamykaj instalatora.",12,FontStyle.Bold,Amber,18));page.Controls.Add(new ProgressBar{Width=PageWidth,Height=Units(20),Style=ProgressBarStyle.Marquee,MarqueeAnimationSpeed=30,Margin=new Padding(0,0,0,Units(18))});page.Controls.Add(Label("Po zakończeniu zobaczysz wynik operacji. Gra nie zostanie uruchomiona.",10,FontStyle.Regular,Muted));}
   if(step==5){
    page.Controls.Add(Label(lastSuccess?(lastRestore?"Oryginalne pliki zostały przywrócone.":"Spolszczenie zostało zainstalowane."):"Nie udało się zakończyć operacji.",14,FontStyle.Bold,lastSuccess?Green:Red,15));if(lastSuccess)page.Controls.Add(Label(operationMessage,12,FontStyle.Bold,Amber,19));if(!lastSuccess)page.Controls.Add(Label(operationMessage,10,FontStyle.Regular,Red,15));
    Card(page,"WERSJA / WERYFIKACJA",Product.Version+" · "+(lastSuccess?"SHA-256 potwierdzony":"operacja wstrzymana"));if(lastSuccess&&!lastRestore){page.Controls.Add(Label("PROJEKT marko22",9,FontStyle.Bold,Amber,5));page.Controls.Add(Label("Spolszczenie jest udostępniane bezpłatnie. Informacje o projektach znajdziesz na marko22.pl. Wsparcie projektu jest dobrowolne.",10,FontStyle.Regular,Muted,12));}page.Controls.Add(Label(lastRestore?"Backupy i historia pozostają zachowane. Gra nie została uruchomiona.":"Wersja 1.0.0. Wybór języka i wynik operacji podano powyżej.",9,FontStyle.Regular,Muted));
   }
  }
  bool InspectPath(){
   try{if(String.IsNullOrWhiteSpace(folder.Text))throw new IOException("Wybierz folder gry.");engine=new Engine(folder.Text,new EmbeddedPatches(Assembly.GetExecutingAssembly()),preferences:new UserLanguage(Path.Combine(folder.Text,Product.StateName),()=>Process.GetProcessesByName("Power to the People").Length>0));pathStatus=engine.Probe();if(pathStatus=="PARTIAL"&&mode.SelectedIndex==0)throw new IOException("Wykryto częściową instalację. Wybierz Usuń / przywróć oryginały.");validationMessage="✓ Gra i wersja zgodne.\n"+(pathStatus=="ORIGINAL"?"Oryginalne pliki gry.":pathStatus=="INSTALLED"?"Spolszczenie już zainstalowane.":pathStatus=="UPGRADE"?"Rozpoznano poprzednie spolszczenie marko22. Instalacja zaktualizuje teksty, zachowując oryginalny backup.":"Częściowy zapis — dostępne przywracanie.");return true;}
   catch(Exception e){pathStatus="";validationMessage=e.Message;return false;}
  }
  void Next(object sender,EventArgs e){if(busy)return;if(step==5){Close();return;}if(step==2&&!InspectPath()){Render();return;}if(step==3){BeginOperation();return;}step++;Render();}
  void BeginOperation(){lastRestore=mode.SelectedIndex==1;bool restore=lastRestore;step=4;busy=true;Render();var worker=new BackgroundWorker();worker.DoWork+=(s,e)=>e.Result=restore?engine.Restore():engine.Install();worker.RunWorkerCompleted+=(s,e)=>{busy=false;lastSuccess=e.Error==null;operationMessage=e.Error==null?Convert.ToString(e.Result):e.Error.Message;step=5;Render();worker.Dispose();};worker.RunWorkerAsync();}
  static bool IsProjectLink(string url){return String.Equals(url,WebsiteUrl,StringComparison.Ordinal)||String.Equals(url,Support,StringComparison.Ordinal);}
  static void LaunchWithShell(string target){Process.Start(new ProcessStartInfo(target){UseShellExecute=true});}
  void OpenLink(string url){if(!IsProjectLink(url))return;try{linkLauncher(url);}catch(Exception e){MessageBox.Show(this,"Nie udało się otworzyć odnośnika: "+e.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  static string ReportPath(){
   byte[] bytes;using(var stream=typeof(Wizard).Assembly.GetManifestResourceStream("Marko22Installer.Kulisy.html")){if(stream==null)throw new IOException("Brak dokumentu Kulisy spolszczenia w instalatorze.");using(var memory=new MemoryStream()){stream.CopyTo(memory);bytes=memory.ToArray();}}
   string hash=Hash.Bytes(bytes),adjacent=Path.Combine(Path.GetDirectoryName(typeof(Wizard).Assembly.Location),ReportFile);
   if(File.Exists(adjacent)&&String.Equals(Hash.FileHash(adjacent),hash,StringComparison.Ordinal))return adjacent;
   // The self-contained EXE also works when moved without the adjacent document.
   string cache=Path.Combine(Path.GetTempPath(),"marko22-PTTP-Kulisy",hash);Directory.CreateDirectory(cache);
   string target=Path.Combine(cache,ReportFile);
   if(!File.Exists(target)||!String.Equals(Hash.FileHash(target),hash,StringComparison.Ordinal))File.WriteAllBytes(target,bytes);
   if(!String.Equals(Hash.FileHash(target),hash,StringComparison.Ordinal))throw new IOException("Nie udało się zapisać kompletnego dokumentu Kulisy spolszczenia.");
   return target;
  }
  void OpenReport(){try{linkLauncher(ReportPath());}catch(Exception e){MessageBox.Show(this,"Nie udało się otworzyć lokalnego dokumentu Kulisy spolszczenia: "+e.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
 }
}
