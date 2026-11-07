using System;
namespace Marko22.PTTP {
 // Exact file pair verified against the preserved UI3 release manifest.
 // Recognizing a release never makes a translated file an original backup.
 internal static class KnownReleases {
  static readonly string[][] Previous={new[]{"1a171d64714106266a347b5e95f82757bdff05b8b985f1d28a6079d4259996c2","c028d6827e817da41a07b8af099758acac76cb61f9ca1c0b538d5501d12f03c8"},new[]{
   "d855f9dc781634bbfc60647e63ffccc06bb55d7f6ca37e81bd616892374600ac",
   "2e88c03ef55e7607d833cc7709c2cbabdbfda18549da62c94a977adc1f81c50a"},new[]{
   "1a171d64714106266a347b5e95f82757bdff05b8b985f1d28a6079d4259996c2",
   "2e88c03ef55e7607d833cc7709c2cbabdbfda18549da62c94a977adc1f81c50a"}};
  public static bool Contains(int index,string hash){foreach(var release in Previous)if(index<release.Length&&String.Equals(release[index],hash,StringComparison.Ordinal))return true;return false;}
  public static bool Matches(string[] hashes){foreach(var release in Previous){if(release.Length!=hashes.Length)continue;bool same=true;for(int i=0;i<hashes.Length;i++)if(!String.Equals(release[i],hashes[i],StringComparison.Ordinal)){same=false;break;}if(same)return true;}return false;}
 }
}
