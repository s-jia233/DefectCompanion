"""Recolour the original visible Spine core while retaining its painted shading and occlusion."""
from pathlib import Path
import json,re,math,sys
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parent
ASSETS=ROOT/'assets';RAW=ROOT/'mod-study/core-raw'
clips={}
for name in ['NativeClips.Generated.cs','CardPoses.Generated.cs']:
    text=(ROOT/name).read_text(encoding='utf8')
    for match in re.finditer(r'clips\.Add\("([^"]+)",new NativeClip\("[^"]+",(\d+),new Rect\(([^)]+)\)',text):
        clips[match[1]]=(int(match[2]),tuple(map(lambda x:int(float(x)),match[3].split(','))))

stops=np.array([0,.18,.38,.58,.78,.90,1.0])
palette=np.array([[19,5,8],[69,7,12],[134,12,16],[198,22,14],[240,44,13],[255,100,16],[255,221,55]])
def material(clip,index,sheets):
    count,(x,y,w,h)=clips[clip]
    sheet=index//64
    if sheet not in sheets:sheets[sheet]=Image.open(ASSETS/f'native-{clip}-{sheet}.png').convert('RGBA')
    n=index%64;tile=sheets[sheet].crop((n%8*w,n//8*h,n%8*w+w,n//8*h+h))
    body=Image.new('RGBA',(320,240));body.paste(tile,(x-50,y-104))
    body=np.asarray(body);core=np.asarray(Image.open(RAW/f'{clip}-{index}.png').convert('RGBA'))
    cover=np.asarray(Image.open(RAW/f'{clip}-cover-{index}.png').convert('RGBA'))
    rgb=body[:,:,:3].astype(float)
    blue=(rgb[:,:,2]>rgb[:,:,0]*1.1)&(rgb[:,:,1]>rgb[:,:,0]*1.1)
    neutral=((rgb.max(2)-rgb.min(2))/np.maximum(1,rgb.max(2))<.46)&(rgb.max(2)<153)
    visible=(blue|neutral)&(body[:,:,3]>0)&(core[:,:,3]>0)
    alpha=np.round(np.minimum(body[:,:,3],core[:,:,3])*visible*(1-cover[:,:,3]/255.0)).astype('uint8')
    light=body[:,:,:3].max(2)/255.0
    red=np.zeros_like(core);hot=np.zeros_like(core)
    for channel in range(3):red[:,:,channel]=np.interp(light,stops,palette[:,channel]).astype('uint8')
    # Keep the original carbon-dark mechanical patch and its painted ink.
    red[:,:,:3][neutral]=body[:,:,:3][neutral]
    red[:,:,3]=alpha
    hot[:,:,:3]=[255,207,50]
    warm=np.clip((light-.60)/.40,0,1);warm=warm*warm*(3-2*warm)
    hot[:,:,3]=np.round(alpha*warm*.75).astype('uint8')
    if alpha.max()>0:
        yy,xx=np.indices(alpha.shape);weight=alpha*(.20+light)
        center=(float((xx*weight).sum()/weight.sum())+50,float((yy*weight).sum()/weight.sum())+104)
    else:center=(210,235)
    return Image.fromarray(red),Image.fromarray(hot),center,int((alpha>32).sum()),int((core[:,:,3]>32).sum())

def pack(name,frames,box):
    w,h=box[2]-box[0],box[3]-box[1]
    for start in range(0,len(frames),64):
        part=frames[start:start+64];sheet=Image.new('RGBA',(w*8,h*math.ceil(len(part)/8)))
        for i,frame in enumerate(part):sheet.paste(frame.crop(box),(i%8*w,i//8*h))
        sheet.save(ASSETS/f'{name}-{start//64}.png',optimize=True)

def main():
    requested=[x for x in sys.argv[1:] if x!='--probe']
    selected=['art-overclock'] if '--probe' in sys.argv else requested or list(clips)
    lines=[];stats={}
    if requested:
        previous=(ROOT/'HotCore.Generated.cs').read_text(encoding='utf8')
        lines=[line for line in previous.splitlines() if line.startswith('Clips.Add(') and re.match(r'Clips.Add\("([^"]+)"',line)[1] not in requested]
        stats=json.loads((ASSETS/'hot-core-sources.json').read_text(encoding='utf8'))['clips']
    for clip in selected:
        count,bounds=clips[clip];sheets={};red=[];hot=[];centers=[];coverage=[]
        for index in range(count):
            image,glow,center,visible,total=material(clip,index,sheets)
            red.append(image);hot.append(glow);centers.append((center,visible>0));coverage.append((visible,total))
        boxes=[im.getbbox() for im in red if im.getbbox()]
        assert boxes,(clip,'no visible original core')
        box=(max(0,min(b[0] for b in boxes)-1),max(0,min(b[1] for b in boxes)-1),min(320,max(b[2] for b in boxes)+1),min(240,max(b[3] for b in boxes)+1))
        stats[clip]={'frames':count,'visible_pixels_max':max(v for v,t in coverage),'visible_pixels_min':min(v for v,t in coverage),'source_core_pixels_max':max(t for v,t in coverage)}
        if '--probe' in sys.argv:
            canvas=Image.new('RGBA',(320,240));count,(x,y,w,h)=clips[clip];frame=30;sheet=Image.open(ASSETS/f'native-{clip}-{frame//64}.png').convert('RGBA');n=frame%64
            canvas.paste(sheet.crop((n%8*w,n//8*h,n%8*w+w,n//8*h+h)),(x-50,y-104))
            canvas=Image.alpha_composite(canvas,red[frame]);canvas.resize((640,480)).save(ROOT/'mod-study/core-probe.png')
            print(json.dumps(stats));return
        pack('native-red-core-'+clip,red,box);pack('native-hot-core-'+clip,hot,box)
        points=','.join('new Point[]{new Point(%.5f,%.5f),new Point(%d,0)}'%(c[0],c[1],int(v)) for c,v in centers)
        lines.append('Clips.Add("%s",new NativeClip("%s",%d,new Rect(%d,%d,%d,%d),new Point[][]{%s}));'%(clip,clip,count,box[0]+50,box[1]+104,box[2]-box[0],box[3]-box[1],points))
    (ROOT/'HotCore.Generated.cs').write_text('using System.Windows;\nnamespace DefectCompanion { public static partial class HotCoreArt { static partial void Bind(){\n'+'\n'.join(lines)+'\n} } }',encoding='utf8')
    report={'source':'Original native Spine orb geometry and original rendered body shading','visible_mask':'Native core alpha with draw-order foreground occlusion per frame; original scarf, arms, grey mechanical patch and armour remain intact','palette':palette.tolist(),'direction':'Predominantly incandescent red, with small yellow highlights on the original brightest facets','frames':sum(v['frames'] for v in stats.values()),'clips':stats,'new_body_or_head_gestures':False}
    (ASSETS/'hot-core-sources.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
    print('Prepared original-core red materials:',report['frames'],'frames /',len(stats),'clips')

if __name__=='__main__':main()
