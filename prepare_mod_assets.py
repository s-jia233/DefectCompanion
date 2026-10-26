"""Prepare user-installed MoreDefectAnimations resources for the standalone desktop renderer."""
import hashlib,json,math,re,shutil,struct,sys
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parent
MOD=Path(r'E:/SteamLibrary/steamapps/workshop/content/2868840/3810696310')
ASSETS=ROOT/'assets'
STUDY=ROOT/'mod-study'
manifest=[]
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def copy(source,name):
    src=MOD/'assets'/source;dst=ASSETS/('mod-'+name+src.suffix)
    shutil.copyfile(src,dst);manifest.append({'source':str(src),'output':dst.name,'sha256':digest(src)})

for name in ['sign','pole','stones','light-field','ground-shadow','fragment-0','fragment-1','fragment-2','fragment-back-0','fragment-back-1','fragment-back-2']:
    copy('buffer/'+name+'.png','buffer-'+name)
for color in ['red','blue']:
    copy('charge-battery/'+color+'-clamp.png','charge-'+color+'-clamp')
    copy('charge-battery/'+color+'-cable.png','charge-'+color+'-cable')
copy('charge-battery/cable-prefabs.bin','cable-prefabs')
for kind in ['snow','mist']:
    sheet=Image.new('RGBA',(128*4,128*4))
    for n in range(16):
        src=MOD/'assets'/'ice-lance'/f'{kind}-{n:02}.png';im=Image.open(src).convert('RGBA');im.thumbnail((128,128),Image.Resampling.LANCZOS)
        sheet.paste(im,(n%4*128+(128-im.width)//2,n//4*128+(128-im.height)//2))
    sheet.save(ASSETS/f'mod-ice-{kind}-0.png')
    manifest.append({'source':str(MOD/'assets'/'ice-lance'/kind)+'-00..15.png','output':f'mod-ice-{kind}-0.png'})

poses=json.loads((STUDY/'head-poses.json').read_text())
generated=[];ice_records=[]
for name,samples in poses.items():
    images=[Image.open(STUDY/'ice-raw'/f'{name}-{i}.png').convert('RGBA') for i in range(len(samples))]
    boxes=[im.getbbox() for im in images if im.getbbox()]
    box=(max(0,min(b[0] for b in boxes)-1),max(0,min(b[1] for b in boxes)-1),min(320,max(b[2] for b in boxes)+1),min(240,max(b[3] for b in boxes)+1))
    w,h=box[2]-box[0],box[3]-box[1]
    for start in range(0,len(images),64):
        part=images[start:start+64];sheet=Image.new('RGBA',(w*8,h*math.ceil(len(part)/8)))
        for i,im in enumerate(part):sheet.paste(im.crop(box),(i%8*w,i//8*h))
        sheet.save(ASSETS/f'mod-ice-{name}-{start//64}.png',optimize=True)
    rows=[]
    for s in samples:
        values=[*s['clamps'][0],*s['clamps'][1],*s['arc'],s['angle']]
        rows.append('new ModHeadPose('+','.join(f'{v:.5f}' for v in values)+')')
    generated.append('HeadPoses.Add("'+name+'",new ModHeadPose[] {'+',\n'.join(rows)+'});')
    ice_records.append('IceClips.Add("%s",new NativeClip("%s",%s,new Rect(%s,%s,%s,%s),null));'%(name,name,len(samples),box[0]+50,box[1]+104,w,h))
(ROOT/'ModBindings.Generated.cs').write_text('using System.Windows;\nnamespace DefectCompanion { public static partial class InstalledModArt { static partial void Bind(){\n'+'\n'.join(generated+ice_records)+'\n} } }',encoding='utf8')
buffer=json.loads((STUDY/'buffer-binding.json').read_text())
rects=[]
for name,b in buffer['rectangles'].items():
    rects.append('BufferRects.Add("%s",new Rect(%s));'%(name,','.join(map(str,b))))
(ROOT/'ModBufferRects.Generated.cs').write_text('using System.Windows;\nnamespace DefectCompanion { public static partial class InstalledModArt { static partial void BindBuffer(){\n'+'\n'.join(rects)+'\n} } }',encoding='utf8')
charge=json.loads((STUDY/'charge-battery-binding.json').read_text())
rests=[]
for cable in charge['cables']:
    ref=cable['referenceMount'];points=[((p['x']-ref['x'])*.085/.3,(p['y']-ref['y'])*.085/.3) for p in cable['bend']['restPoints']]
    rests.append('new Point[] {'+','.join('new Point(%.6f,%.6f)'%p for p in points)+'}')
(ROOT/'ModCableRests.Generated.cs').write_text('using System.Windows;\nnamespace DefectCompanion { public static partial class InstalledModArt { public static readonly Point[][] CableRests=new Point[][] {'+','.join(rests)+'}; } }',encoding='utf8')
source=(STUDY/'decompiled'/'MoreDefectAnimations'/'TurboNativeRig.cs').read_text(encoding='utf8')
script=json.loads(re.search(r'private const string Source = ("(?:\\.|[^"\\])*");',source).group(1))
(STUDY/'turbo_native.gd').write_text(script,encoding='utf8')
(ASSETS/'mod-sources.json').write_text(json.dumps({'mod':json.loads((MOD/'mod_manifest.json').read_text()),'assembly_sha256':digest(MOD/'MoreDefectAnimations.dll'),'resources':manifest,'ice_frame_count':sum(map(len,poses.values())),'defragment_frame_count':108,'notes':'Local personal-use adaptation. No mod assembly is loaded or distributed by the pet. Read-only source inspection; source game and subscription files unchanged.'},ensure_ascii=False,indent=2),encoding='utf8')
print('Prepared',len(manifest),'resource groups and',sum(map(len,poses.values())),'weighted ice frames')
