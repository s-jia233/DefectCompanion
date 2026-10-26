"""Build previews from actual deterministic desktop renderer frames."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parent
QA=ROOT.parents[1]/'outputs'/'defect-desktop-v23-preview'
FRAMES=QA/'frames'
FONT=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',19)
def tile(skill,n,label):
    detail=skill=='neck-detail'
    if detail:skill='machine_learning'
    count=len(list(FRAMES.glob('action-'+skill+'-[0-9][0-9][0-9].png')))
    im=Image.open(FRAMES/f'action-{skill}-{min(n,count-1):03}.png').convert('RGBA')
    out=Image.new('RGB',(420,416),(24,31,40))
    if detail:
        im=im.crop((170,106,258,226)).resize((264,360),Image.Resampling.LANCZOS);out.paste(im,(78,43),im)
    else:out.paste(im,(0,26),im)
    ImageDraw.Draw(out).text((12,1),label,font=FONT,fill=(220,223,234));return out

def make(name,cards):
    frames=[]
    for n in range(210):
        out=Image.new('RGB',(420*len(cards),416),(24,31,40))
        for i,(skill,label) in enumerate(cards):out.paste(tile(skill,n,label),(i*420,0))
        frames.append(out)
    sample=Image.new('RGB',(frames[0].width,416*9))
    for i,n in enumerate([0,12,24,32,43,65,95,135,165]):sample.paste(frames[n],(0,i*416))
    palette=sample.quantize(colors=256,method=Image.Quantize.MEDIANCUT)
    indexed=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
    indexed[0].save(QA/(name+'.gif'),save_all=True,append_images=indexed[1:],duration=[30,30,40]*70,loop=0,disposal=2,optimize=True)
    frames[30].save(QA/(name+'.png'))
    print(name,(QA/(name+'.gif')).stat().st_size)
make('充能球弧形排列',[('orb-arc-three','默认 · 三球弧形排列'),('orb-arc-five','扩容 · 五球弧形排列')])
