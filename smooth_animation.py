"""Build eight motion-compensated samples per original animation frame.

This is an offline asset build; the desktop executable never needs OpenCV.
Colors are warped in premultiplied-alpha space to keep transparent edges clean.
Original keyframes and finite-action timing are preserved exactly.
"""
import hashlib
import json
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT/'.build-tools'))
import cv2

COUNTS = [6,8,8,4,5,8,6,6,6]
LOOPING = {0,1,2,6,7}
SAMPLES = 8
W,H = 192,208

def feature(image):
    rgba=np.asarray(image,dtype=np.float32)/255
    a=rgba[:,:,3]
    # Shape boundaries and internal ink both guide the motion field.
    grey=cv2.cvtColor(rgba[:,:,:3],cv2.COLOR_RGB2GRAY)
    return np.ascontiguousarray(np.clip((grey*a*.65+a*.35)*255,0,255).astype(np.uint8))

def flow(a,b):
    estimator=cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
    estimator.setUseSpatialPropagation(True)
    return estimator.calc(feature(a),feature(b),None)

def interpolate(a,b,ab,ba,t):
    yy,xx=np.mgrid[:H,:W].astype(np.float32)
    pa=np.asarray(a,dtype=np.float32)/255
    pb=np.asarray(b,dtype=np.float32)/255
    pa[:,:,:3]*=pa[:,:,3:4];pb[:,:,:3]*=pb[:,:,3:4]
    wa=cv2.remap(pa,xx-ab[:,:,0]*t,yy-ab[:,:,1]*t,cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
    wb=cv2.remap(pb,xx-ba[:,:,0]*(1-t),yy-ba[:,:,1]*(1-t),cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
    out=wa*(1-t)+wb*t
    out[:,:,:3]/=np.maximum(out[:,:,3:4],1/255)
    out[out[:,:,3]<1/255]=0
    return Image.fromarray(np.clip(out*255+.5,0,255).astype(np.uint8),'RGBA')

def main():
    atlas=Image.open(ROOT/'pet.png').convert('RGBA')
    dest=ROOT/'assets'; total=0; manifests=[]
    qa=ROOT.parents[1]/'outputs'/'defect-desktop-v3.2-preview';qa.mkdir(parents=True,exist_ok=True)
    comparison=Image.new('RGB',(W*8,H*3),(24,32,42))
    for row,count in enumerate(COUNTS):
        keys=[atlas.crop((i*W,row*H,(i+1)*W,(row+1)*H)) for i in range(count)]
        samples=[]
        for index,a in enumerate(keys):
            b=keys[(index+1)%count] if row in LOOPING or index<count-1 else a
            same=index==count-1 and row not in LOOPING
            ab=flow(a,b) if not same else None
            ba=flow(b,a) if not same else None
            for sub in range(SAMPLES):
                im=a.copy() if sub==0 or same else interpolate(a,b,ab,ba,sub/SAMPLES)
                samples.append(im)
                if row in (0,3,4) and index==1:
                    comparison.paste(im,(sub*W,(0 if row==0 else 1 if row==3 else 2)*H),im)
        sheet=Image.new('RGBA',(W*8,H*((len(samples)+7)//8)))
        for index,im in enumerate(samples):sheet.paste(im,((index%8)*W,(index//8)*H))
        path=dest/('motion-'+str(row)+'.png');sheet.save(path,optimize=True)
        # Each unmodified source keyframe must remain pixel-identical in the output.
        for i,key in enumerate(keys): assert np.array_equal(np.asarray(samples[i*SAMPLES]),np.asarray(key))
        if row not in LOOPING:assert all(np.array_equal(np.asarray(im),np.asarray(keys[-1])) for im in samples[-SAMPLES:])
        manifests.append(dict(row=row,source_frames=count,samples=len(samples),looping=row in LOOPING,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
        total+=len(samples);print('Animation',row,len(samples),'frames',flush=True)
        if row in (0,3,4):
            # Slow sample loop for visual inspection, rendered against a neutral background.
            frames=[]
            for im in samples:
                bg=Image.new('RGBA',im.size,(24,32,42,255));bg.alpha_composite(im);frames.append(bg.convert('RGB'))
            frames[0].save(qa/('motion-'+str(row)+'.gif'),save_all=True,append_images=frames[1:],duration=30,loop=0,disposal=2)
    comparison.save(qa/'motion-inbetweens.png')
    data=dict(source_sha256=hashlib.sha256((ROOT/'pet.png').read_bytes()).hexdigest(),method='bidirectional DIS optical flow / premultiplied RGBA',samples_per_keyframe=SAMPLES,total_motion_samples=total,original_keyframes_preserved=True,finite_actions_hold_last_frame=True,animations=manifests)
    (dest/'motion.json').write_text(json.dumps(data,indent=2),encoding='utf8')
    print('Built',total,'motion samples; all original keyframes verified')
    import build_run_cycle
    build_run_cycle.main()

if __name__=='__main__':main()
