"""Read selected public-format Godot resources from the local, unmodified game PCK."""
import argparse
import hashlib
import io
import json
import re
import struct
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent
GAME = Path(r'E:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck')

def index_pack(path=GAME):
    with path.open('rb') as f:
        def u32(): return struct.unpack('<I', f.read(4))[0]
        def u64(): return struct.unpack('<Q', f.read(8))[0]
        assert f.read(4) == b'GDPC'
        version, major, minor, patch, flags = [u32() for _ in range(5)]
        if flags & 1: raise ValueError('Encrypted directories are not supported')
        base = u64()
        if version == 3: f.seek(u64())
        elif version == 2: f.seek(64, 1)
        else: raise ValueError(f'Unsupported pack {version}')
        count = u32()
        entries = {}
        for _ in range(count):
            name = f.read(u32()).decode('utf-8').rstrip('\0')
            offset, size = u64(), u64()
            digest, file_flags = f.read(16).hex(), u32()
            entries[name] = {'offset':base+offset,'size':size,'md5':digest,'flags':file_flags}
        return entries

def read_asset(entries, name):
    item=entries[name]
    if item['flags'] & 1: raise ValueError('Encrypted files are not supported')
    with GAME.open('rb') as f:
        f.seek(item['offset']); data=f.read(item['size'])
    assert hashlib.md5(data).hexdigest()==item['md5'], name
    return data

_cache = {}
def texture(entries, name):
    name=name.removeprefix('res://')
    if name in _cache: return _cache[name].copy()
    if name.endswith('.tres'):
        text=read_asset(entries,name).decode()
        source=re.search(r'path="res://([^"]+)"',text).group(1)
        rect=tuple(map(int,re.search(r'region = Rect2\(([^)]+)\)',text).group(1).split(',')))
        im=texture(entries,source).crop((rect[0],rect[1],rect[0]+rect[2],rect[1]+rect[3]))
    else:
        if name+'.import' in entries:
            text=read_asset(entries,name+'.import').decode()
            name2=re.search(r'^path(?:\.[a-z0-9_]+)?="res://([^"]+)"',text,re.M).group(1)
        else: name2=name
        data=read_asset(entries,name2)
        pos=data.find(b'RIFF')
        if pos>=0:
            size=struct.unpack_from('<I',data,pos+4)[0]+8
            im=Image.open(io.BytesIO(data[pos:pos+size])).convert('RGBA')
        elif b'\x89PNG' in data:
            im=Image.open(io.BytesIO(data[data.find(b'\x89PNG'):])).convert('RGBA')
        elif data.startswith(b'GST2'):
            data_format=struct.unpack_from('<I',data,36)[0]
            w,h=struct.unpack_from('<HH',data,40)
            fmt=struct.unpack_from('<I',data,48)[0]
            if data_format==0 and fmt in (17,18,19,22):
                # Standard BC7 blocks wrapped in a DDS DX10 header for Pillow's decoder.
                fourcc={17:b'DXT1',18:b'DXT3',19:b'DXT5',22:b'DX10'}[fmt]
                block_size=8 if fmt==17 else 16
                header=[124,0x81007,h,w,((w+3)//4)*((h+3)//4)*block_size,0,1]+[0]*11+[32,4,struct.unpack('<I',fourcc)[0],0,0,0,0,0]+[0x1000,0,0,0,0]
                dds=b'DDS '+struct.pack('<31I',*header)+(struct.pack('<5I',98,3,0,1,0) if fmt==22 else b'')+data[52:]
                im=Image.open(io.BytesIO(dds)).convert('RGBA')
            else: raise ValueError(f'Unsupported GPU image: data={data_format},fmt={fmt}')
        else: raise ValueError(f'No image in {name2}')
    _cache[name]=im.copy()
    return im

def extract_selected():
    entries=index_pack(); dest=ROOT/'assets'; dest.mkdir(exist_ok=True)
    paths={
        'orb-lightning':'images/packed/orbs/lightning_orb.png',
        'orb-frost':'images/packed/orbs/frost_orb.png',
        'orb-dark':'images/packed/orbs/dark_orb.png',
        'orb-plasma':'images/packed/orbs/plasma_orb.png',
    }
    # The installed build can move source folders; match the exact basename uniquely.
    for key in list(paths):
        basename=paths[key].split('/')[-1]+'.import'
        matches=[n[:-7] for n in entries if n.endswith('/'+basename)]
        assert len(matches)==1,(key,matches)
        paths[key]=matches[0]
    catalog=ROOT/'card_catalog.json'
    names=[c['asset'] for c in json.loads(catalog.read_text(encoding='utf8'))] if catalog.exists() else ['zap','dualcast','charge_battery','coolheaded','darkness','fusion','rainbow','hologram','echo_form','rocket_punch','leap','reboot']
    for name in names:
        paths['art-'+name]='images/packed/card_portraits/defect/'+name+'.png'
    for kind in ['skill','attack','power']:
        paths['frame-'+kind]='images/atlases/ui_atlas.sprites/card/card_frame_'+kind+'_s.tres'
        paths['border-'+kind]='images/atlases/ui_atlas.sprites/card/card_portrait_border_'+kind+'_s.tres'
    paths['card-banner']='images/atlases/ui_atlas.sprites/card/card_banner.tres'
    paths['energy-defect']='images/atlases/ui_atlas.sprites/card/energy_defect.tres'
    for kind in ['lightning','frost','dark','plasma']:
        paths['particle-'+kind]='images/vfx/orbs/'+kind+'_orb_particle.png'
    for n in ['lightning_flipbook_1','lightning_flipbook_2']:
        paths[n]='images/vfx/lightning/'+n+'.png'
    for n in ['00','01b','02','03','04']:
        paths['bolt-'+n]='images/vfx/vfx_lightning/vfx_lightning_'+n+'_crop.png'
    manifest=[]
    for key,path in paths.items():
        try:
            im=texture(entries,path)
            im.save(dest/(key+'.png'))
            manifest.append({'name':key,'source':path,'size':im.size,'sha256':hashlib.sha256((dest/(key+'.png')).read_bytes()).hexdigest()})
            print(key,im.size)
        except Exception as error: print('UNAVAILABLE',key,error)
    body=texture(entries,'animations/characters/defect/defect.png')
    body.crop((719,54,782,109)).save(dest/'cast-fist.png')
    plaque=texture(entries,'images/ui/cards/card_portrait_border_plaque2.png')
    plaque.save(dest/'type-plaque.png')
    for name,path,crop in [('cast-fist','animations/characters/defect/defect.png',[719,54,63,55]),('type-plaque','images/ui/cards/card_portrait_border_plaque2.png',None)]:
        manifest.append({'name':name,'source':path,'crop':crop,'sha256':hashlib.sha256((dest/(name+'.png')).read_bytes()).hexdigest()})
    (dest/'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')

if __name__=='__main__':
    entries=index_pack()
    (ROOT/'game-index.json').write_text(json.dumps(entries),encoding='utf8')
    matching=[n for n in entries if re.search(r'orb|/defect/|card.*(frame|base)|lightning|frost|hologram|dualcast|charge_battery|rocket_punch',n,re.I)]
    (ROOT/'asset-candidates.txt').write_text('\n'.join(matching),encoding='utf8')
    print('Pack entries:',len(entries))
    print('\n'.join(matching[:100]))
    extract_selected()
