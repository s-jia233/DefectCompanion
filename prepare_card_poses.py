"""Pack articulated original-rig card gestures together with their head and clamp bindings."""
import json,math,sys,re
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parent;ASSETS=ROOT/'assets';RAW=ROOT/'mod-study/art-raw'
poses=json.loads((ROOT/'mod-study/art-poses.json').read_text())
designs=json.loads((ROOT/'art-direction.json').read_text(encoding='utf8'))
native=[];heads=[];mounts=[];counts={}
def pack(name,frames):
    boxes=[f.getbbox() for f in frames if f.getbbox()]
    box=(max(0,min(b[0] for b in boxes)-1),max(0,min(b[1] for b in boxes)-1),min(320,max(b[2] for b in boxes)+1),min(240,max(b[3] for b in boxes)+1))
    w,h=box[2]-box[0],box[3]-box[1]
    for start in range(0,len(frames),64):
        part=frames[start:start+64];sheet=Image.new('RGBA',(w*8,h*math.ceil(len(part)/8)))
        for n,f in enumerate(part):sheet.paste(f.crop(box),(n%8*w,n//8*h))
        sheet.save(ASSETS/f'{name}-{start//64}.png',optimize=True)
    return f'new Rect({box[0]+50},{box[1]+104},{w},{h})'
selected=set(sys.argv[1:])
for name,samples in poses.items():
    if selected and name not in selected:continue
    frames=[];blue=[];rows=[];binding=[]
    for i,p in enumerate(samples):
        im=Image.open(RAW/f'{name}-{i}.png').convert('RGBA');frames.append(im)
        head=np.array(Image.open(RAW/f'{name}-head-{i}.png').convert('RGBA'));body=np.array(im)
        visible=(np.abs(head[:,:,:3].astype(float)-body[:,:,:3]).max(2)<24)&(body[:,:,3]>0)
        l=(head[:,:,0]*.25+head[:,:,1]*.60+head[:,:,2]*.15)/255;shade=np.clip((l-.08)/.84,0,1)
        out=np.zeros_like(head);out[:,:,0]=13+shade*95;out[:,:,1]=67+shade*151;out[:,:,2]=112+shade*140
        out[:,:,3]=np.round(np.minimum(head[:,:,3],body[:,:,3])*visible*np.clip((l-.18)/.17,0,1));blue.append(Image.fromarray(out))
        rows.append('new Point[] {'+','.join('new Point(%.5f,%.5f)'%tuple(q) for q in p['anchors'])+'}')
        values=[*p['clamps'][0],*p['clamps'][1],*p['arc'],p['angle']]
        binding.append('new ModHeadPose('+','.join('%.5f'%v for v in values)+')')
    bounds=pack('native-'+name,frames);cold_bounds=pack('mod-cold-head-'+name,blue)
    native.append('clips.Add("%s",new NativeClip("%s",%d,%s,new Point[][] {%s}));'%(name,name,len(samples),bounds,',\n'.join(rows)))
    heads.append('ColdHeadClips.Add("%s",new NativeClip("%s",%d,%s,null));'%(name,name,len(samples),cold_bounds))
    mounts.append('HeadPoses.Add("%s",new ModHeadPose[] {%s});'%(name,',\n'.join(binding)))
    counts[name]=len(samples)
    # Confirm the requested palm positions are actually reached, not just stored in configuration.
    skill=next(k for k,v in designs.items() if v['clip']==name);c=designs[skill]
    if c['hands']:
        frame=samples[round(.5*60)]
        error=max(math.dist((frame['anchors'][i][0]+50,frame['anchors'][i][1]+104),c['hands'][i]) for i in range(2))
        assert error<10,(name,'IK hand target mismatch',error)
target=ROOT/'CardPoses.Generated.cs'
if selected:
    previous=target.read_text(encoding='utf8')
    for name in selected:
        for prefix in ['clips.Add','ColdHeadClips.Add','HeadPoses.Add']:
            previous=re.sub(re.escape(prefix)+r'\("'+re.escape(name)+r'".*?;\n?', '',previous,flags=re.S)
    previous=previous.replace('\n} } public static partial class InstalledModArt', '\n'+'\n'.join(native)+'\n} } public static partial class InstalledModArt')
    assert previous.endswith('\n} } }')
    previous=previous[:-6]+'\n'+'\n'.join(heads+mounts)+'\n} } }'
    target.write_text(previous,encoding='utf8')
    old=json.loads((ASSETS/'card-pose-sources.json').read_text(encoding='utf8'))['clips'];old.update(counts);counts=old
else:
    target.write_text('using System.Windows;\nnamespace DefectCompanion { public sealed partial class NativeAnimation { private void LoadCardPoses(){\n'+'\n'.join(native)+'\n} } public static partial class InstalledModArt { static partial void BindCardPoses(){\n'+'\n'.join(heads+mounts)+'\n} } }',encoding='utf8')
(ASSETS/'card-pose-sources.json').write_text(json.dumps({'fps':60,'frames':sum(counts.values()),'clips':counts,'designs':designs,'renderer':'Original locally installed Spine runtime, analytic two-bone arm gestures, aligned head and clamp attachments'},ensure_ascii=False,indent=2),encoding='utf8')
print('Prepared',sum(counts.values()),'art-directed original-rig frames with moving attachments')
