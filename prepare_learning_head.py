"""Bake card-palette circuitry into the existing visible native head masks."""
from pathlib import Path
import json,re,math
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parent;ASSETS=ROOT/'assets'
poses=json.loads((ROOT/'mod-study/head-poses.json').read_text())
poses.update(json.loads((ROOT/'mod-study/art-poses.json').read_text()))
clips={}
for source in ['ColdHead.Generated.cs','CardPoses.Generated.cs']:
    for m in re.finditer(r'ColdHeadClips.Add\("([^"]+)",new NativeClip\("[^"]+",(\d+),new Rect\(([^)]+)\)',(ROOT/source).read_text(encoding='utf8')):
        clips[m[1]]=(int(m[2]),tuple(int(float(v)) for v in m[3].split(',')))
paths=[[(-19,-23),(-6,-23),(-6,-13),(1,-13)],[(7,-25),(12,-25),(12,-14),(20,-14),(20,-5)],[(-23,-3),(-14,-3),(-14,5),(-7,5)],[(-12,10),(-1,10),(-1,17),(11,17)]]
for name,(count,(x,y,w,h)) in clips.items():
    for start in range(0,count,64):
        source=Image.open(ASSETS/f'mod-cold-head-{name}-{start//64}.png').convert('RGBA')
        material=Image.new('RGBA',source.size);traces=Image.new('RGBA',source.size)
        for n in range(min(64,count-start)):
            px=n%8*w;py=n//8*h;rgba=np.array(source.crop((px,py,px+w,py+h)))
            shade=np.clip((rgba[:,:,0].astype(float)-13)/95,0,1);out=rgba.copy()
            for c,(dark,span) in enumerate([(8,64),(75,165),(79,145)]):out[:,:,c]=np.clip(dark+shade*span,0,255)
            p=poses[name][start+n];head=p.get('head',p.get('anchors',[None]*4)[3]);cx,cy=head[0]+50-x,head[1]+104-y
            cs,sn=math.cos(p['angle']),math.sin(p['angle'])
            trace=Image.new('L',(w*4,h*4));d=ImageDraw.Draw(trace)
            for path in paths:
                points=[((cx+a*cs-b*sn)*4,(cy+a*sn+b*cs)*4) for a,b in path]
                d.line(points,fill=255,width=6,joint='curve')
                ax,ay=points[-1];d.ellipse((ax-5,ay-5,ax+5,ay+5),fill=255)
            alpha=np.array(trace.resize((w,h),Image.Resampling.LANCZOS)).astype(float)*rgba[:,:,3]/255
            line=np.zeros_like(rgba);line[:,:,:3]=[197,255,235];line[:,:,3]=np.round(alpha*.94).astype('uint8')
            material.paste(Image.fromarray(out),(px,py));traces.paste(Image.fromarray(line),(px,py))
        material.save(ASSETS/f'mod-learning-head-{name}-{start//64}.png',optimize=True)
        traces.save(ASSETS/f'mod-learning-trace-{name}-{start//64}.png',optimize=True)
report={'reference':'Original locally installed machine_learning and fusion card illustrations','method':'Original shaded visible native head recoloured teal, with pale circuit tracks clipped by the same pose-specific head alpha; eye, scarf and ink apertures retained','clips':len(clips),'frames':sum(c for c,b in clips.values()),'deformation':'Every material frame and trace frame matches the corresponding native head pose; circuit coordinates rotate with the evaluated head binding','persistent_while_learning':True,'frost_head_has_priority':True,'new_native_gestures':{'art-learning':156,'art-fusion':150},'learning':'Claw touches temple; original neck extends and the two original rear pipes stretch from fixed scarf-side bases as the rigid head rises 18 units; native visible neck, pipes and head show teal circuits; no added appendages; later focus gain pulses the head traces','fusion':'Ice-blue crystalline current, gold lightning and violet dark current converge into a teal multicell core; warm compression flash and one native plasma orb inserted at 1.52 seconds','fusion_orb_birth_seconds':1.52,'fusion_rules_unchanged':True,'independent_echo_resolution':True}
(ASSETS/'learning-fusion-sources.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print('Prepared circuit heads:',report['frames'],'frames /',len(clips),'clips')
