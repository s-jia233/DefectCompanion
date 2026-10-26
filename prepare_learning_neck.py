"""Recolour the real native neck and rear pipes with their exact foreground occlusion."""
from pathlib import Path
import json,math
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parent;RAW=ROOT/'mod-study/learning-neck-raw';ASSETS=ROOT/'assets'
materials=[];traces=[];visible=[]
for frame in range(156):
    material=Image.new('RGBA',(320,240));trace=Image.new('RGBA',(320,240))
    for part in [2,0]:
        source=np.array(Image.open(RAW/f'{frame}-{part}.png').convert('RGBA'));cover=np.array(Image.open(RAW/f'{frame}-{part+1}.png').convert('RGBA'))
        alpha=np.round(source[:,:,3]*(1-cover[:,:,3]/255.0)).astype('uint8')
        light=source[:,:,:3].mean(2)/255;shade=np.clip(light/.43,0,1)
        out=np.zeros_like(source)
        for c,(dark,span) in enumerate([(5,44),(31,167),(39,148)]):out[:,:,c]=np.clip(dark+shade*span,0,255)
        out[:,:,3]=alpha
        ink=Image.new('L',(320*3,240*3));d=ImageDraw.Draw(ink)
        box=Image.fromarray(alpha).getbbox()
        if box:
            x0,y0,x1,y1=box;cx=(x0+x1)/2
            if part==0:
                points=[(cx-2,y1),(cx-2,y0+(y1-y0)*.65),(cx+3,y0+(y1-y0)*.65),(cx+3,y0)]
                d.line([(x*3,y*3) for x,y in points],fill=225,width=3,joint='curve')
                for y in np.arange(y0+4,y1-1,5):
                    d.line(((x0+2)*3,y*3,(x1-2)*3,(y-1.0)*3),fill=200,width=3)
            else:
                for y in np.arange(y0+3,y1,5):d.line((x0*3,y*3,x1*3,(y-1.0)*3),fill=205,width=3)
            line=np.zeros_like(source);line[:,:,:3]=[176,255,229]
            line[:,:,3]=np.round(np.array(ink.resize((320,240),Image.Resampling.LANCZOS)).astype(float)/255*alpha*np.clip((light-.025)/.09,0,1)).astype('uint8')
            trace=Image.alpha_composite(trace,Image.fromarray(line))
        material=Image.alpha_composite(material,Image.fromarray(out))
    materials.append(material);traces.append(trace);visible.append(int((np.array(material)[:,:,3]>32).sum()))
boxes=[f.getbbox() for f in materials if f.getbbox()]
box=(max(0,min(b[0] for b in boxes)-1),max(0,min(b[1] for b in boxes)-1),min(320,max(b[2] for b in boxes)+1),min(240,max(b[3] for b in boxes)+1))
w,h=box[2]-box[0],box[3]-box[1]
for name,frames in [('mod-learning-neck',materials),('mod-learning-neck-trace',traces)]:
    for start in range(0,len(frames),64):
        part=frames[start:start+64];sheet=Image.new('RGBA',(w*8,h*math.ceil(len(part)/8)))
        for i,im in enumerate(part):sheet.paste(im.crop(box),(i%8*w,i//8*h))
        sheet.save(ASSETS/f'{name}-{start//64}.png',optimize=True)
(ROOT/'LearningNeck.Generated.cs').write_text('using System.Windows;\nnamespace DefectCompanion { public static partial class LearningNeckArt { public static readonly Rect Bounds=new Rect(%d,%d,%d,%d); } }'%(box[0]+50,box[1]+104,w,h),encoding='utf8')
report={'native_slots':['neck','pipe 1','pipe 2'],'frames':156,'head_rise_stage_units':18,'neck_base_fixed':True,'rear_pipe_bases_fixed':True,'head_scale_unchanged':True,'visible_pixels_min':min(visible),'visible_pixels_max':max(visible),'material_bounds':[box[0]+50,box[1]+104,w,h],'occlusion':'Separate native neck/pipe layers masked by their actual later draw-order slots; original head, scarf, arms and armour remain in front','added_appendages':0,'fusion_unchanged':True}
(ASSETS/'learning-neck-sources.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print('Prepared native neck and rear-pipe circuit material:',report['frames'],'frames',report['material_bounds'])
