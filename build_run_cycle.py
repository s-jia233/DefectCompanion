"""Rebuild a coherent planted-foot run from rigid pieces of one existing pose.

No optical flow is used for the run or charging pose. A fixed torso and two-bone
legs avoid silhouette/scale oscillation; the final-to-first sample is continuous.
"""
import hashlib
import json
import math
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'.build-tools'))
import cv2

W,H=192,208
QA=ROOT.parents[1]/'outputs'/'defect-desktop-v3.2-preview'
SOURCE=Image.open(ROOT/'pet.png').convert('RGBA')
POSE=SOURCE.crop((0,H,W,H*2))

def part(polygons):
    mask=Image.new('L',(W,H));draw=ImageDraw.Draw(mask)
    for polygon in polygons:draw.polygon(polygon,fill=255)
    out=POSE.copy();out.putalpha(Image.fromarray((np.asarray(mask,dtype=np.uint16)*np.asarray(POSE)[:,:,3]//255).astype('uint8')))
    return out

def transform(image,source,target,angle=0):
    matrix=cv2.getRotationMatrix2D(tuple(source),-angle,1)
    matrix[:,2]+=np.asarray(target)-np.asarray(source)
    data=np.asarray(image,dtype=np.float32)/255
    data[:,:,:3]*=data[:,:,3:4]
    out=cv2.warpAffine(data,matrix,(W,H),flags=cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
    out[:,:,:3]/=np.maximum(out[:,:,3:4],1/255)
    return Image.fromarray(np.clip(out*255+.5,0,255).astype('uint8'),'RGBA')

def bone(image,a,b,start,end):
    original=math.atan2(b[1]-a[1],b[0]-a[0])
    target=math.atan2(end[1]-start[1],end[0]-start[0])
    return transform(image,a,start,math.degrees(target-original))

def joint_cap():
    # Original overlapping armor at the knee covers the pivots during rotation.
    mask=Image.new('L',(W,H));ImageDraw.Draw(mask).ellipse((112,138,132,158),fill=255)
    out=POSE.copy();out.putalpha(Image.fromarray((np.asarray(mask,dtype=np.uint16)*np.asarray(POSE)[:,:,3]//255).astype('uint8')))
    return out

def knee(hip,ankle,l1,l2):
    delta=np.asarray(ankle)-np.asarray(hip);distance=float(np.linalg.norm(delta))
    direction=delta/distance
    along=(l1*l1-l2*l2+distance*distance)/(2*distance)
    across=math.sqrt(max(0,l1*l1-along*along))
    # Both legs bend forward in screen space; opposite phases alternate the feet.
    return np.asarray(hip)+direction*along+np.asarray([direction[1],-direction[0]])*across

def ankle(hip,phase):
    phase%=1
    if phase<.6: # Stance: planted foot travels backwards relative to the moving body.
        x=18-36*phase/.6;y=179
        toe=0
    else:
        t=(phase-.6)/.4;ease=t*t*(3-2*t)
        x=-18+36*ease;y=179-20*math.sin(t*math.pi)**2
        toe=-12*math.sin(t*math.pi)
    return np.asarray([hip[0]+x,y]),toe

def build_run():
    torso=part([
        [(0,0),(192,0),(192,107),(126,107),(124,131),(105,137),(78,137),(70,130),(70,108),(0,108)],
        [(28,99),(64,99),(67,119),(62,136),(31,136)],
        [(119,99),(177,99),(177,140),(141,140),(125,128)],
    ])
    definitions=[
        dict(hip=(85,128),joint=(68,148),foot=(42,165),
             thigh=[(70,120),(96,125),(87,146),(73,158),(55,155),(52,140)],
             shin=[(45,139),(79,141),(82,158),(59,172),(40,167),(34,151)],
             boot=[(26,138),(48,140),(57,160),(49,184),(28,182),(24,165)],offset=.5,angle=-65),
        dict(hip=(111,133),joint=(125,154),foot=(131,180),
             thigh=[(101,124),(121,127),(135,145),(124,158),(108,151),(96,135)],
             shin=[(114,140),(137,140),(146,178),(135,188),(112,179),(108,155)],
             boot=[(114,177),(146,176),(158,185),(166,200),(113,200)],offset=0,angle=0),
    ]
    for leg in definitions:
        for name in ['thigh','shin','boot']:leg[name]=part([leg[name]])
        leg['l1']=math.dist(leg['hip'],leg['joint']);leg['l2']=math.dist(leg['joint'],leg['foot'])
    frames=[];trajectory=[];cap=joint_cap()
    for i in range(64):
        phase=i/64
        bob=-1.1*math.sin(phase*2*math.pi)**2
        canvas=Image.new('RGBA',(W,H));positions=[]
        for leg in definitions:
            hip=np.asarray(leg['hip'],dtype=float)+[0,bob]
            foot,toe=ankle(hip,phase+leg['offset'])
            joint=knee(hip,foot,leg['l1'],leg['l2'])
            canvas.alpha_composite(transform(cap,(122,148),hip))
            canvas.alpha_composite(transform(cap,(122,148),joint))
            canvas.alpha_composite(bone(leg['thigh'],leg['hip'],leg['joint'],hip,joint))
            canvas.alpha_composite(bone(leg['shin'],leg['joint'],leg['foot'],joint,foot))
            canvas.alpha_composite(transform(leg['boot'],leg['foot'],foot,leg['angle']+toe))
            positions.append(dict(hip=hip.tolist(),knee=joint.tolist(),ankle=foot.tolist()))
        canvas.alpha_composite(transform(torso,(100,100),(100,100+bob)))
        frames.append(canvas);trajectory.append(positions)
    return frames,trajectory

def save(row,frames):
    sheet=Image.new('RGBA',(W*8,H*((len(frames)+7)//8)))
    for i,frame in enumerate(frames):sheet.paste(frame,((i%8)*W,(i//8)*H))
    path=ROOT/'assets'/('motion-'+str(row)+'.png');sheet.save(path,optimize=True)
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    QA.mkdir(parents=True,exist_ok=True)
    frames,trajectory=build_run()
    left=[f.transpose(Image.Transpose.FLIP_LEFT_RIGHT) for f in frames]
    hashes={1:save(1,frames),2:save(2,left)}
    # Charge uses one stable core-focused pose; it no longer pumps the head/arm.
    charge=SOURCE.crop((W,7*H,2*W,8*H))
    hashes[7]=save(7,[charge.copy() for _ in range(48)])
    contact=Image.new('RGB',(W*8,H*2),(24,32,42))
    for j,seq in enumerate([frames,left]):
        for i in range(8):contact.paste(seq[i*8],(i*W,j*H),seq[i*8])
    contact.save(QA/'run-cycle-contact.png')
    for name,seq in [('run-right',frames),('run-left',left)]:
        preview=[]
        for frame in seq:
            bg=Image.new('RGBA',(W,H),(24,32,42,255));bg.alpha_composite(frame);preview.append(bg.convert('RGB'))
        preview[0].save(QA/(name+'.gif'),save_all=True,append_images=preview[1:],duration=20,loop=0,disposal=2)
    manifest=ROOT/'assets/motion.json'
    data=json.loads(manifest.read_text(encoding='utf8'))
    data['original_keyframes_preserved']=False
    data['run_revision']={'method':'single-pose torso / two-bone legs / alternating planted-foot gait','period_seconds':.9,'run_samples':64,'maximum_torso_bob_pixels':1.1,'direction_pair':'exact horizontal mirror','charging_pose':'stable core hold','optical_flow_used_for_run':False}
    for row in data['animations']:
        if row['row'] in hashes:
            row['sha256']=hashes[row['row']]
            row['method']='rigged planted-foot gait' if row['row'] in (1,2) else 'stable core hold'
    manifest.write_text(json.dumps(data,indent=2),encoding='utf8')
    # The cyclic endpoint must move no further than an ordinary adjacent step.
    positions=np.asarray([[leg['ankle'] for leg in sample] for sample in trajectory])
    travel=np.linalg.norm(np.roll(positions,-1,axis=0)-positions,axis=2)
    assert float(travel[-1].max())<2
    assert np.allclose(positions[np.arange(64)/64<.6,1,1],179)
    report={'right_and_left_frames':128,'torso_fixed_shape':True,'maximum_ankle_step_pixels':float(travel.max()),'loop_seam_ankle_step_pixels':float(travel[-1].max()),'stance_ankle_height_pixels':179,'direction_images_are_exact_mirrors':all(np.array_equal(np.asarray(a)[:,::-1],np.asarray(b)) for a,b in zip(frames,left))}
    (QA/'run-asset-checks.json').write_text(json.dumps(report,indent=2),encoding='utf8')
    print(json.dumps(report,indent=2))

if __name__=='__main__':main()
