"""Read original power icons and card VFX; bake their shader tint for WPF."""
import hashlib,json,sys,math
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
import extract_game_assets as game

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'.build-tools'))
import cv2
ASSETS=ROOT/'assets'
ICONS=['focus','storm','thunder','loop','buffer','echo_form','creative_ai','machine_learning','energy_next_turn']
TEXTURES={
 'hyper-core':'hyperbeam/hyperbeam_core.png',
 'hyper-charge':'hyperbeam/hyperbeam_laser_anticipation.png',
 'hyper-line':'hyperbeam/hyperbeam_laser_line.png',
 'hyper-overlay':'hyperbeam/hyperbeam_laser_overlay.png',
 'sweep':'sweeping_beam/sweeping_beam_flipbook_quick.png',
 'slash':'slash/slash_quick_flat.png',
 'fire':'fire_impact/fire_burst_flipbook_1.png',
 'rock':'vfx_molten_fist/vfx_rock_light_ver.png',
 'debris':'vfx_rock_shatter/vfx_rock_shatter_rock.png',
 'smoke':'vfx_rock_shatter/vfx_rock_shatter_smoke.png',
 'flare':'common/common_clean_flare_1.png',
 'spark':'common/common_glow_speck.png',
 'power-ring':'vfx_power_up/base_ring.png',
 'power-beam':'vfx_power_up/beam.png',
 'block':'vfx_block/vfx_block_02.png',
 'frost':'orbs/frost_orb_particle.png',
 'dark':'orbs/dark_orb_particle.png',
 'plasma':'orbs/plasma_orb_particle.png',
 'lightning':'orbs/lightning_orb_particle.png',
}
COLORS={
 'hyper-core':[(.38,.78,1),(1,1,1)],
 'hyper-charge':[(.6,.88,1),(1,1,1)],
 'hyper-line':[(0,.20,.85),(0,.70,1),(1,1,1)],
 'hyper-overlay':[(0,.38,.8),(.6,.9,1),(1,1,1)],
 'sweep':[(1,.24,.02),(1,.5,.05),(1,.74,.2)],
 'slash':[(1,.35,.15),(1,.83,.55),(1,1,.95)],
 'fire':[(.85,.12,.025),(1,.48,.05),(1,.97,.65)],
 'smoke':[(.18,.22,.28),(.36,.41,.48)],
 'flare':[(.32,.63,1),(1,1,1)],
 'spark':[(.23,.65,.95),(1,1,1)],
 'power-ring':[(.15,.55,.82),(.70,.94,1)],
 'power-beam':[(.10,.40,.68),(.5,.91,1)],
 'block':[(.07,.25,.52),(.22,.62,.9),(.85,.98,1)],
 'frost':[(.1,.5,.85),(.48,.84,1),(.93,.99,1)],
 'dark':[(.2,.05,.4),(.55,.18,.85),(.88,.66,1)],
 'plasma':[(.8,.2,.01),(1,.65,.08),(1,.96,.65)],
 'lightning':[(.2,.5,.9),(.5,.85,1),(1,1,1)],
}

def tint(im,stops):
    a=np.asarray(im.convert('RGBA')).copy();v=a[:,:,0].astype(float)/255
    # Additive black masks need intensity in alpha under WPF's ordinary blending.
    a[:,:,3]=(a[:,:,3].astype(float)*v).astype('uint8')
    coords=np.linspace(0,1,len(stops))
    for c in range(3):a[:,:,c]=np.interp(v,coords,[s[c]*255 for s in stops]).astype('uint8')
    return Image.fromarray(a)

def project_sweep(image):
    # Reproduce the native material's 67 degree pseudo-3D projection and UV mask.
    yy,xx=np.mgrid[:256,:512].astype('float32');u=(xx+.5)/512;v=(yy+.5)/256
    sine=math.sin(math.radians(67));cosine=math.cos(math.radians(67))
    z=sine*(v-.5)+cosine*.5
    pu=(u-.5)*cosine/np.maximum(z,.0001)+.5
    pv=(cosine*(v-.5)-sine*.5)*cosine/np.maximum(z,.0001)+sine+.5
    valid=(z>0)&(pu>=0)&(pu<=1)&(pv>=0)&(pv<=1)
    mask=np.clip((pv-.33760685)/(.42307693-.33760685),0,1)*valid
    result=Image.new('RGBA',(1536,512));w,h=image.width//3,image.height//2
    for i in range(6):
        cell=np.asarray(image.crop((i%3*w,i//3*h,i%3*w+w,i//3*h+h)),dtype='float32')/255
        cell[:,:,:3]*=cell[:,:,3:4]
        pixels=cv2.remap(cell,(pu*(w-1)).astype('float32'),(pv*(h-1)).astype('float32'),cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
        pixels[:,:,:3]/=np.maximum(pixels[:,:,3:4],1/255);pixels[:,:,3]*=mask
        frame=Image.fromarray(np.clip(pixels*255,0,255).astype('uint8'))
        result.paste(frame,(i%3*512,i//3*256))
    return result

def normalise_fire(image):
    result=Image.new('RGBA',(384,256));w,h=image.width//3,image.height//2
    for i in range(6):
        cell=image.crop((i%3*w,i//3*h,i%3*w+w,i//3*h+h))
        box=cell.getchannel('A').point(lambda x:255 if x>12 else 0).getbbox()
        if box:
            cell=cell.crop(box);scale=min(116/cell.width,116/cell.height)
            cell=cell.resize((max(1,round(cell.width*scale)),max(1,round(cell.height*scale))),Image.Resampling.LANCZOS)
            result.paste(cell,(i%3*128+(128-cell.width)//2,i//3*128+(128-cell.height)//2))
    return result

def main():
    entries=game.index_pack();records=[];previews=[]
    paths={'power-'+n:'images/powers/'+n+'_power.png' for n in ICONS}
    paths['power-capacity']='images/relics/runic_capacitor.png'
    paths.update({'fx-'+n:'images/vfx/'+p for n,p in TEXTURES.items()})
    for key,path in paths.items():
        im=game.texture(entries,path)
        if key.startswith('fx-') and key[3:] in COLORS:im=tint(im,COLORS[key[3:]])
        if key=='fx-sweep':im=project_sweep(im)
        if key=='fx-fire':im=normalise_fire(im)
        out=ASSETS/(key+'.png');im.save(out)
        records.append(dict(key=key,source=path,size=im.size,sha256=hashlib.sha256(out.read_bytes()).hexdigest(),processing='original image' if key.startswith('power-') else 'original shape with desktop-compatible shader palette'))
        previews.append((key,im.copy()));print(key,im.size,flush=True)
    (ASSETS/'style-sources.json').write_text(json.dumps(records,indent=2),encoding='utf8')
    qa=ROOT.parents[1]/'outputs'/'defect-desktop-v4-preview';qa.mkdir(parents=True,exist_ok=True)
    sheet=Image.new('RGB',(800,((len(previews)+4)//5)*148),(19,27,39));d=ImageDraw.Draw(sheet)
    for i,(name,im) in enumerate(previews):
        im.thumbnail((145,116));x=i%5*160;y=i//5*148
        sheet.paste(im,(x+(160-im.width)//2,y+20),im);d.text((x+4,y+3),name,fill='white')
    sheet.save(qa/'original-style-assets.png')
    print('Verified and prepared',len(records),'original resources')

if __name__=='__main__':main()
