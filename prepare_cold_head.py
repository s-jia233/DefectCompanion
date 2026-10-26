"""Make a shaded blue overlay from the visible original head, with exact pose alignment."""
import json,math
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parent
ASSETS=ROOT/'assets'
native=json.loads((ASSETS/'native-animation.json').read_text())
poses=json.loads((ROOT/'mod-study/head-poses.json').read_text())
rows=[];report={};total=0
for name,samples in poses.items():
    info=native[name];bounds=info['bounds'];w0,h0=bounds[2]-bounds[0],bounds[3]-bounds[1]
    frames=[];errors=[]
    for i,s in enumerate(samples):
        errors.append(max(abs(s['head'][j]-info['anchors'][i][3][j]) for j in range(2)))
        if i%64==0:sheet=Image.open(ASSETS/f'native-{name}-{i//64}.png').convert('RGBA')
        body=Image.new('RGBA',(320,240));body.paste(sheet.crop((i%8*w0,i%64//8*h0,i%8*w0+w0,i%64//8*h0+h0)),bounds[:2])
        head=np.array(Image.open(ROOT/'mod-study/head-raw'/f'{name}-{i}.png').convert('RGBA'))
        rgba=np.array(body);a=head[:,:,3].astype(float)/255
        # Later body slots (cowl, scarf, hands) occlude the isolated head.
        difference=np.abs(head[:,:,:3].astype(float)-rgba[:,:,:3]).max(2)
        visible=(difference<24)&(rgba[:,:,3]>0)
        # Antialias coverage from the complete native body keeps silhouette edges intact.
        alpha=np.minimum(head[:,:,3],rgba[:,:,3])*visible
        l=(head[:,:,0]*.25+head[:,:,1]*.60+head[:,:,2]*.15)/255
        shade=np.clip((l-.08)/.84,0,1)
        out=np.zeros_like(head)
        out[:,:,0]=np.clip(13+shade*95,0,255)
        out[:,:,1]=np.clip(67+shade*151,0,255)
        out[:,:,2]=np.clip(112+shade*140,0,255)
        # Keep the dark eye aperture and inked creases from the native head.
        metal=np.clip((l-.18)/.17,0,1)
        out[:,:,3]=np.round(alpha*metal)
        frames.append(Image.fromarray(out))
    assert max(errors)<.02,(name,max(errors))
    boxes=[im.getbbox() for im in frames if im.getbbox()]
    assert boxes,name
    box=(max(0,min(b[0] for b in boxes)-1),max(0,min(b[1] for b in boxes)-1),min(320,max(b[2] for b in boxes)+1),min(240,max(b[3] for b in boxes)+1))
    w,h=box[2]-box[0],box[3]-box[1]
    for start in range(0,len(frames),64):
        part=frames[start:start+64];packed=Image.new('RGBA',(w*8,h*math.ceil(len(part)/8)))
        for n,im in enumerate(part):packed.paste(im.crop(box),(n%8*w,n//8*h))
        packed.save(ASSETS/f'mod-cold-head-{name}-{start//64}.png',optimize=True)
    rows.append('ColdHeadClips.Add("%s",new NativeClip("%s",%d,new Rect(%d,%d,%d,%d),null));'%(name,name,len(frames),box[0]+50,box[1]+104,w,h))
    report[name]={'frames':len(frames),'distinct_head_angles':len(set(round(s['angle'],5) for s in samples)),'max_native_anchor_error_pixels':max(errors),'bounds':box}
    total+=len(frames)
(ROOT/'ColdHead.Generated.cs').write_text('using System.Windows;\nnamespace DefectCompanion { public static partial class InstalledModArt { static partial void BindColdHead(){\n'+'\n'.join(rows)+'\n} } }',encoding='utf8')
(ROOT/'mod-study/head-fix-verification.json').write_text(json.dumps({'total_frames':total,'clips':report},indent=2),encoding='utf8')
print('Prepared',total,'visible blue head frames; all anchors match native rendering within 0.02 px')
