"""Original PTTPPLP1 binary-delta encoder used by release 1.0.0."""
import gzip,struct
def delta(a,b):
 # Exact 64-byte matches, indexed every 16 source bytes; every match is byte-compared.
 index={}
 for i in range(0,len(a)-63,16):index.setdefault(a[i:i+64],[]).append(i)
 ops=[];pos=0;literal=bytearray()
 while pos<len(b):
  hits=index.get(b[pos:pos+64],[]) if pos+64<=len(b) else []
  best=None
  for off in hits[:32]:
   end=64
   while pos+end+256<=len(b) and off+end+256<=len(a) and a[off+end:off+end+256]==b[pos+end:pos+end+256]:end+=256
   while pos+end<len(b) and off+end<len(a) and a[off+end]==b[pos+end]:end+=1
   if best is None or end>best[1]:best=(off,end)
  if best:
   if literal:ops.append((1,bytes(literal)));literal.clear()
   ops.append((0,best));pos+=best[1]
  else:literal.append(b[pos]);pos+=1
 if literal:ops.append((1,bytes(literal)))
 raw=bytearray(b'PTTPPLP1'+struct.pack('<III',len(a),len(b),len(ops)))
 for typ,v in ops:
  raw.append(typ)
  if typ==0:raw.extend(struct.pack('<II',*v))
  else:raw.extend(struct.pack('<I',len(v))+v)
 return gzip.compress(raw,compresslevel=9,mtime=0),len(ops)
