"""Local serialized-file reader/writer; no network, API, or game execution."""
import struct
from pathlib import Path

class Reader:
    def __init__(self, data, pos=0): self.data, self.pos = data, pos
    def read(self, n):
        assert 0 <= n <= len(self.data)-self.pos, (self.pos, n, len(self.data))
        v=self.data[self.pos:self.pos+n]; self.pos+=n; return v
    def num(self, fmt): return struct.unpack('<'+fmt, self.read(struct.calcsize('<'+fmt)))[0]
    def align(self, n=4): self.pos=(self.pos+n-1)//n*n
    def z(self):
        end=self.data.index(b'\0',self.pos); v=self.data[self.pos:end].decode('utf8'); self.pos=end+1; return v
    def string(self):
        v=self.read(self.num('i')).decode('utf8'); self.align(); return v

def string(value):
    b=value.encode('utf8'); b=struct.pack('<i',len(b))+b
    return b+b'\0'*(-len(b)%4)

class SerializedFile:
    def __init__(self, path):
        self.path=Path(path); self.data=self.path.read_bytes()
        self.version=struct.unpack_from('>I',self.data,8)[0]
        assert self.version==22
        self.metadata_size, size, self.data_offset=struct.unpack_from('>IQQ',self.data,20)
        assert size==len(self.data)
        r=Reader(self.data,48)
        self.unity_version=r.z(); r.num('i'); self.type_tree=r.num('?'); self.types=[]
        for _ in range(r.num('i')):
            cid=r.num('i'); r.num('?'); script_index=r.num('h')
            if cid==114: r.read(16)
            r.read(16)
            if self.type_tree:
                nodes=r.num('i'); text=r.num('i'); r.read(nodes*32+text); r.read(r.num('i')*4)
            self.types.append({'class_id':cid,'script_index':script_index})
        self.objects=[]
        for _ in range(r.num('i')):
            r.align(); pid=r.num('q'); entry=r.pos; start=r.num('Q'); length=r.num('I'); tid=r.num('i')
            o={'path_id':pid,'entry':entry,'offset':self.data_offset+start,'size':length,**self.types[tid]}
            raw=self.raw(o)
            try:
                if o['class_id']==114:
                    q=Reader(raw,16); o['script_file']=q.num('i'); o['script_id']=q.num('q')
                    o['name']=q.string(); o['payload']=q.pos
                elif o['class_id'] in (1,21,28,49,115,128): o['name']=Reader(raw).string()
            except (ValueError,UnicodeError,AssertionError,struct.error): pass
            self.objects.append(o)
        # Local serialized identifiers, followed by external file references.
        for _ in range(r.num('i')):
            r.num('i'); r.align(); r.num('q')
        self.externals=[]
        for _ in range(r.num('i')):
            r.z(); r.read(16); r.num('i'); self.externals.append(r.z())
    def raw(self,o): return self.data[o['offset']:o['offset']+o['size']]
    def save_replacing(self, path, replacements):
        header=bytearray(self.data[:self.data_offset]); body=bytearray()
        for o in sorted(self.objects,key=lambda x:x['offset']):
            body.extend(b'\0'*(-len(body)%8)); start=len(body)
            body.extend(replacements.get(o['path_id'],self.raw(o)))
            struct.pack_into('<QI',header,o['entry'],start,len(body)-start)
        struct.pack_into('>Q',header,24,len(header)+len(body))
        Path(path).write_bytes(header+body)

def parse_i2(raw, payload):
    r=Reader(raw,payload)
    agreed=[r.num('i') for _ in range(3)]; terms_start=r.pos
    terms=[]
    for _ in range(r.num('i')):
        key=r.string(); typ=r.num('i'); values=[r.string() for _ in range(r.num('i'))]
        flags=list(r.read(r.num('i'))); r.align(); touch=[r.string() for _ in range(r.num('i'))]
        terms.append(dict(key=key,type=typ,values=values,flags=flags,touch=touch))
    terms_end=r.pos; case=r.num('B'); r.align(); missing=r.num('i'); app=r.string()
    languages_start=r.pos
    langs=[]
    for _ in range(r.num('i')):
        name=r.string(); code=r.string(); flags=r.num('B'); r.align(); langs.append(dict(name=name,code=code,flags=flags))
    languages_end=r.pos
    return dict(terms=terms,languages=langs,terms_start=terms_start,terms_end=terms_end,
                languages_start=languages_start,languages_end=languages_end,case_insensitive=case,
                missing_action=missing,app_term=app,agreement_flags=agreed)

def font_cmap(raw):
    """Find an embedded SFNT and read its Unicode cmap, without rasterization."""
    cursor=0
    while True:
        candidates=[x for x in (raw.find(b'\x00\x01\x00\x00',cursor),raw.find(b'OTTO',cursor)) if x>=0]
        if not candidates: return None
        start=min(candidates)
        cursor=start+4
        if start+12>len(raw): continue
        count=struct.unpack_from('>H',raw,start+4)[0]
        if not 3<=count<=64 or start+12+16*count>len(raw): continue
        tables={}
        for i in range(count):
            tag,check,off,size=struct.unpack_from('>4sIII',raw,start+12+i*16)
            tables[tag]=(start+off,size)
        if not {b'cmap',b'head',b'maxp'}.issubset(tables): continue
        cmap,size=tables[b'cmap']
        if cmap+size>len(raw): continue
        chars=set()
        for i in range(struct.unpack_from('>H',raw,cmap+2)[0]):
            plat,enc,off=struct.unpack_from('>HHI',raw,cmap+4+i*8)
            if plat not in (0,3): continue
            p=cmap+off; fmt=struct.unpack_from('>H',raw,p)[0]
            if fmt==4:
                seg=struct.unpack_from('>H',raw,p+6)[0]//2
                end=p+14; begin=end+seg*2+2; delta=begin+seg*2; ro=delta+seg*2
                for j in range(seg):
                    a=struct.unpack_from('>H',raw,begin+j*2)[0]; z=struct.unpack_from('>H',raw,end+j*2)[0]
                    d=struct.unpack_from('>h',raw,delta+j*2)[0]; offset=struct.unpack_from('>H',raw,ro+j*2)[0]
                    for cp in range(a,z+1):
                        if cp==65535: continue
                        glyph=struct.unpack_from('>H',raw,ro+j*2+offset+(cp-a)*2)[0] if offset else cp
                        if (glyph+d)&65535 if (glyph or not offset) else 0: chars.add(cp)
            elif fmt==12:
                for j in range(struct.unpack_from('>I',raw,p+12)[0]):
                    a,z,glyph=struct.unpack_from('>III',raw,p+16+j*12)
                    chars.update(range(a+(glyph==0),z+1))
        return {'offset':start,'tables':len(tables),'characters':chars}

def parse_tmp(raw,payload):
    r=Reader(raw,payload)
    r.read(20)  # TMP_Asset: hash, material pointer, material hash
    version=r.string(); guid=r.string(); source=(r.num('i'),r.num('q')); mode=r.num('i')
    r.num('i'); family=r.string(); style=r.string(); r.num('i'); r.read(16*4)
    glyph_count=r.num('i'); assert 0<=glyph_count<100000
    r.read(glyph_count*48)
    char_count=r.num('i'); assert 0<=char_count<100000
    chars=[]
    for _ in range(char_count):
        typ=r.num('i'); cp=r.num('I'); glyph=r.num('I'); scale=r.num('f'); chars.append(cp)
    return dict(version=version,source=source,mode=mode,family=family,style=style,
                glyph_count=glyph_count,characters=set(chars))
