"""Apply or generate PTTPPLP1 deltas on copies. No network or installation."""
import argparse,gzip,hashlib,json,struct
from pathlib import Path
from delta import delta
ROOT=Path(__file__).resolve().parents[1]
sha=lambda b:hashlib.sha256(b).hexdigest()

def apply_bytes(original,patch):
    raw=gzip.decompress(patch)
    if raw[:8]!=b'PTTPPLP1':raise ValueError('Unknown patch format')
    src_size,dst_size,count=struct.unpack_from('<III',raw,8)
    if src_size!=len(original):raise ValueError('Original size mismatch')
    pos=20;output=bytearray()
    for _ in range(count):
        typ=raw[pos];pos+=1
        if typ==0:
            off,n=struct.unpack_from('<II',raw,pos);pos+=8
            if off+n>len(original):raise ValueError('Copy outside original')
            output.extend(original[off:off+n])
        elif typ==1:
            n=struct.unpack_from('<I',raw,pos)[0];pos+=4
            if pos+n>len(raw):raise ValueError('Incomplete literal')
            output.extend(raw[pos:pos+n]);pos+=n
        else:raise ValueError('Unknown operation')
        if len(output)>dst_size:raise ValueError('Output too large')
    if pos!=len(raw) or len(output)!=dst_size:raise ValueError('Incomplete patch')
    return bytes(output)

def main():
    p=argparse.ArgumentParser(description=__doc__);sub=p.add_subparsers(dest='command',required=True)
    for command in ('apply','create'):
        s=sub.add_parser(command);s.add_argument('--game-data',required=True);s.add_argument('--output',required=True)
        if command=='create':s.add_argument('--localized-data',required=True)
    a=p.parse_args();original=Path(a.game_data).resolve();out=Path(a.output).resolve()
    if out.exists() or out==original or original in out.parents or out in original.parents:raise ValueError('Use a separate new output directory')
    payload=ROOT/'installer/payload';manifest=json.loads((payload/'manifest.json').read_text(encoding='utf-8'))
    pending=[];new_entries=[]
    for entry in manifest['entries']:
        relative=Path(entry['path']).relative_to('Power to the People_Data')
        source=(original/relative).read_bytes()
        if sha(source)!=entry['original_sha256']:raise ValueError('Unknown original: '+str(relative))
        if a.command=='apply':
            patch=(payload/entry['patch_file']).read_bytes()
            if sha(patch)!=entry['patch_sha256']:raise ValueError('Corrupt patch')
            target=apply_bytes(source,patch)
            if sha(target)!=entry['installed_sha256']:raise ValueError('Unexpected localized result')
            pending.append((relative,target))
        else:
            target=(Path(a.localized_data)/relative).read_bytes();patch,operations=delta(source,target)
            if apply_bytes(source,patch)!=target:raise ValueError('Delta roundtrip failed')
            pending.append((Path(entry['patch_file']),patch))
            new_entries.append({**entry,'installed_sha256':sha(target),'installed_size':len(target),'patch_sha256':sha(patch),'patch_size':len(patch),'operations':operations})
    out.mkdir(parents=True)
    for rel,data in pending:
        dest=out/rel;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
    if a.command=='create':
        manifest.update(entries=new_entries,status='DEVELOPMENT_REQUIRES_QA')
        (out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        entries=[]
        for e in new_entries:
            entries.append('new Entry("'+e['path'].replace('/','\\\\')+'", "'+e['original_sha256']+'", "'+e['installed_sha256']+'", "'+e['patch_sha256']+'", "'+e['patch_file']+'", '+str(e['original_size'])+', '+str(e['installed_size'])+')')
        source='namespace Marko22.PTTP { internal static class Product { public const string Version="'+manifest['version']+'";public const string ExeHash="'+manifest['game_exe_sha256']+'";public const string StateName=".marko22-pttp-pl-rc1";public static readonly Entry[] Entries=new Entry[]{'+',\n'.join(entries)+'};}}\n'
        (out/'Product.cs').write_text(source,encoding='utf-8')
    print('Created '+str(out))

if __name__=='__main__':main()
