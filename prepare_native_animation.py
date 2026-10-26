"""Pack read-only native Spine renders, preserving the exact 60 Hz samples and bone anchors."""
import json, math
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parent
ASSETS=ROOT/'assets'
data=json.loads((ASSETS/'native-animation.json').read_text())
generated=[]
for name,clip in data.items():
    frames=[]
    for i in range(clip['count']):
        if i%64==0: sheet=Image.open(ASSETS/f'native-{name}-{i//64}.png').convert('RGBA')
        frames.append(sheet.crop((i%8*320,i%64//8*240,i%8*320+320,i%64//8*240+240)))
    boxes=[f.getbbox() for f in frames if f.getbbox()]
    box=(max(0,min(b[0] for b in boxes)-1),max(0,min(b[1] for b in boxes)-1),min(320,max(b[2] for b in boxes)+1),min(240,max(b[3] for b in boxes)+1))
    w,h=box[2]-box[0],box[3]-box[1]
    for start in range(0,len(frames),64):
        part=frames[start:start+64]; packed=Image.new('RGBA',(w*8,h*math.ceil(len(part)/8)))
        for i,f in enumerate(part): packed.paste(f.crop(box),(i%8*w,i//8*h))
        packed.save(ASSETS/f'native-{name}-{start//64}.png',optimize=True)
    clip['bounds']=list(box)
    anchors=',\n'.join('            new Point[] { '+', '.join('new Point(%s,%s)'%(p[0],p[1]) for p in a)+' }' for a in clip['anchors'])
    generated.append('''            clips.Add("%s",new NativeClip("%s",%s,new Rect(%s,%s,%s,%s),new Point[][] {
%s
            }));'''%(name,name,clip['count'],box[0]+50,box[1]+104,w,h,anchors))
(ROOT/'NativeClips.Generated.cs').write_text('''using System.Windows;
namespace DefectCompanion {
    public sealed partial class NativeAnimation {
        private void LoadClips() {
%s
        }
    }
}
'''%'\n'.join(generated),encoding='utf8')
data['_source']={'game_build':'v0.111.0','renderer':'Original locally installed Spine runtime, manual 1/60-second steps','original_animations':list(data),'reference_mod':'https://steamcommunity.com/sharedfiles/filedetails/?id=3810696310','mod_files_used':False}
(ASSETS/'native-animation.json').write_text(json.dumps(data,ensure_ascii=False),encoding='utf8')
print('Packed',sum(c['count'] for k,c in data.items() if k!='_source'),'native frames')
