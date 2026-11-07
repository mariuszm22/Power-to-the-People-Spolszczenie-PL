"""Build the Polish I2 column in a new file; never write to the original."""
import argparse,hashlib,json,struct
from pathlib import Path
from unity_lokalnie import SerializedFile,parse_i2,string

ROOT=Path(__file__).resolve().parents[1]
def rebuild(source,output,translations):
    source,output=Path(source).resolve(),Path(output).resolve()
    if output.exists() or source==output:raise ValueError('Output must be a new file')
    manifest=json.loads((ROOT/'installer/payload/manifest.json').read_text(encoding='utf-8'))
    expected=next(x for x in manifest['entries'] if x['patch_file']=='i2.patch.gz')
    if hashlib.sha256(source.read_bytes()).hexdigest()!=expected['original_sha256']:raise ValueError('Unknown original resources.assets')
    records=json.loads(Path(translations).read_text(encoding='utf-8'))
    by={x['id']:x for x in records}
    if len(records)!=1677 or len(by)!=1677:raise ValueError('Expected 1677 unique records')
    s=SerializedFile(source);obj=next(x for x in s.objects if x['path_id']==1027)
    raw=s.raw(obj);table=parse_i2(raw,obj['payload'])
    if len(table['terms'])!=1677:raise ValueError('Unexpected I2 record count')
    langs=table['languages'];en=next(i for i,x in enumerate(langs) if x['code']=='en')
    if any(x['code']=='pl' for x in langs):raise ValueError('Original must not already contain Polish')
    terms=bytearray(struct.pack('<i',len(table['terms'])))
    for t in table['terms']:
        record=by[t['key']]
        if t['values'][en]!=record['en']:raise ValueError('EN conflict: '+t['key'])
        values=t['values']+[record['pl']];flags=t['flags']+[0]
        terms.extend(string(t['key'])+struct.pack('<ii',t['type'],len(values)))
        for value in values:terms.extend(string(value))
        terms.extend(struct.pack('<i',len(flags))+bytes(flags))
        terms.extend(b'\0'*(-len(terms)%4));terms.extend(struct.pack('<i',len(t['touch'])))
        for value in t['touch']:terms.extend(string(value))
    languages=bytearray(struct.pack('<i',len(langs)+1))
    for lang in langs+[{'name':'Polish','code':'pl','flags':0}]:
        languages.extend(string(lang['name'])+string(lang['code'])+bytes([lang['flags']])+b'\0'*3)
    result=raw[:table['terms_start']]+terms+raw[table['terms_end']:table['languages_start']]+languages+raw[table['languages_end']:]
    output.parent.mkdir(parents=True,exist_ok=True)
    s.save_replacing(output,{1027:result})
    return output

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('original');p.add_argument('output')
    p.add_argument('--translations',default=str(ROOT/'translation/EN_PL_1.0.0.json'))
    a=p.parse_args();out=rebuild(a.original,a.output,a.translations)
    print('Created '+str(out))
