"""Compose the game's original card layers with its Chinese titles and rules text."""
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import extract_game_assets as source

ROOT=Path(__file__).resolve().parent
ASSETS=ROOT/'assets'
CARDS=[
 ('zap','电击','skill','basic','1','生成1个闪电充能球。'),
 ('dualcast','双重释放','skill','basic','1','激发你最右侧的充能球两次。'),
 ('charge_battery','充电','skill','common','1','获得7点格挡。\n在下个回合获得1点能量。'),
 ('coolheaded','冷静头脑','skill','common','1','生成1个冰霜充能球。\n抽1张牌。'),
 ('darkness','漆黑','skill','uncommon','1','生成1个黑暗充能球。\n触发所有黑暗充能球的被动。'),
 ('fusion','聚变','skill','uncommon','2','生成1个等离子充能球。'),
 ('rainbow','彩虹','skill','rare','2','生成1个闪电充能球。\n生成1个冰霜充能球。\n生成1个黑暗充能球。\n消耗。'),
 ('hologram','全息影像','skill','common','1','获得3点格挡。\n将弃牌堆中的一张牌放入你的手牌。\n消耗。'),
 ('echo_form','回响形态','power','rare','3','你每回合打出的第一张牌会被打出两次。\n虚无。'),
 ('rocket_punch','火箭飞拳','attack','uncommon','2','造成13点伤害。\n抽1张牌。\n每当你生成状态牌时，此牌的耗能将在下一次打出前减少1点能量。'),
 ('leap','飞跃','skill','common','1','获得9点格挡。'),
 ('reboot','重启','skill','rare','0','将你的所有未消耗的卡牌重新洗牌放入抽牌堆。\n抽4张牌。\n消耗。'),
]

def game_shader(im,h=1,s=1,v=1):
    rgba=np.asarray(im).copy()
    rgb=rgba[:,:,:3].astype(float)/255
    mat=np.array([[.2989,.5870,.1140],[.5959,-.2774,-.3216],[.2115,-.5229,.3114]])
    yiq=rgb@mat.T
    angle=(1-h)*2*math.pi
    c,z=math.cos(angle),math.sin(angle)
    hue=np.array([[1,0,0],[0,c,z],[0,-z,c]])
    yiq=(yiq@hue)*np.array([v,s*v,s*v])
    rgba[:,:,:3]=np.clip((yiq@np.linalg.inv(mat).T)*255,0,255).astype('uint8')
    return Image.fromarray(rgba)

def font(size,bold=False):
    return ImageFont.truetype(r'C:\Windows\Fonts\msyhbd.ttc' if bold else r'C:\Windows\Fonts\msyh.ttc',size)

def wrapped(text,f,maxw):
    lines=[]
    for paragraph in text.split('\n'):
        row=''
        for char in paragraph:
            if f.getlength(row+char)>maxw and row: lines.append(row);row=''
            row+=char
        lines.append(row)
    return lines

def main():
    entries=source.index_pack()
    localization=json.loads(source.read_asset(entries,'localization/zhs/cards.json'))
    manifest=[]
    catalog=ROOT/'card_catalog.json'
    rows=json.loads(catalog.read_text(encoding='utf8')) if catalog.exists() else []
    cards=[(c['asset'],c['name'],c['kind'],c['rarity'],c['cost'],c['effect']) for c in rows] if rows else CARDS
    for ident,title,kind,rarity,cost,description in cards:
        assert localization[ident.upper()+'.title']==title
        frame=game_shader(Image.open(ASSETS/('frame-'+kind+'.png')).convert('RGBA'),.55,.9,1)
        banner=Image.open(ASSETS/'card-banner.png').convert('RGBA')
        border=Image.open(ASSETS/('border-'+kind+'.png')).convert('RGBA')
        if rarity in ('basic','common'):
            banner=game_shader(banner,1,0,.85);border=game_shader(border,1,0,.85)
        elif rarity=='rare':
            banner=game_shader(banner,.563,1.198,1.14);border=game_shader(border,.563,1.198,1.14)
        card=Image.new('RGBA',(654,900))
        card.alpha_composite(Image.open(ASSETS/('art-'+ident+'.png')).resize((500,380),Image.Resampling.LANCZOS),(77,114))
        card.alpha_composite(frame.resize((600,844)),(27,28))
        card.alpha_composite(border.resize((550,420)),(52,122))
        card.alpha_composite(banner.resize((654,145)),(0,36))
        plaque=Image.open(ASSETS/'type-plaque.png').resize((150,74),Image.Resampling.LANCZOS)
        card.alpha_composite(plaque,(252,452))
        icon=Image.open(ASSETS/'energy-defect.png').resize((124,124),Image.Resampling.LANCZOS)
        card.alpha_composite(icon,(0,0))
        draw=ImageDraw.Draw(card)
        draw.text((327,73),title,font=font(48,True),anchor='mm',fill='#fff6e2',stroke_width=4,stroke_fill='#504b40')
        draw.text((62,60),cost,font=font(57,True),anchor='mm',fill='#fff6e2',stroke_width=4,stroke_fill='#23394d')
        draw.text((327,487),{'skill':'技能','attack':'攻击','power':'能力'}[kind],font=font(29,True),anchor='mm',fill='#2b3035')
        draw.text((327,558),'桌宠效果',font=font(26),anchor='mm',fill='#b6c8cb')
        for size in range(38,26,-1):
            f=font(size);lines=wrapped(description,f,475)
            if len(lines)*(size+10)<=210:break
        y=697-len(lines)*(size+10)/2
        for line in lines:
            draw.text((327,y+size/2),line,font=f,anchor='mm',fill='#fff6e2',stroke_width=1,stroke_fill='#343139');y+=size+10
        card.save(ASSETS/('card-'+ident+'.png'))
        manifest.append({'id':ident,'title':title,'kind':kind,'cost':cost,'desktop_effect':description,'original_art':'art-'+ident+'.png'})
    (ASSETS/'cards.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
    # The three-by-three lightning sheet is a red-channel shader mask in the game.
    for n in [1,2]:
        im=np.asarray(Image.open(ASSETS/f'lightning_flipbook_{n}.png').convert('RGBA')).copy()
        im[:,:,3]=(im[:,:,3].astype('uint16')*im[:,:,0].astype('uint16')//255).astype('uint8')
        im[:,:,:3]=[160,225,255]
        Image.fromarray(im).save(ASSETS/f'lightning-sequence-{n}.png')
    print('Composed',len(cards),'cards from original layers; original Chinese names verified')

if __name__=='__main__':main()
