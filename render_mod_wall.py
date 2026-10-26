"""Desktop raster adaptation of the installed mod's authored 98-tile layout and scan graph."""
from pathlib import Path
import math,struct
import numpy as np
from PIL import Image,ImageDraw,ImageFilter

ROOT=Path(__file__).resolve().parent
SOURCE=Path(r'E:/SteamLibrary/steamapps/workshop/content/2868840/3810696310/assets/defragment/geometry.bin')
b=SOURCE.read_bytes();head=struct.unpack_from('<14i2f',b);w,h=struct.unpack_from('<2i',b,64)
records=np.frombuffer(b,dtype='<f4',offset=72,count=w*h*4).reshape(-1,4)
S=2;W,H=320,267;scale=head[15]*W/528
def ease(a,b,x):
    q=max(0,min(1,(x-a)/(b-a)));return q*q*(3-2*q)
def clip(poly,a,b,c):
    out=[]
    for p,q in zip(poly,poly[1:]+poly[:1]):
        v=a*p[0]+b*p[1]+c;u=a*q[0]+b*q[1]+c
        if v>=-1e-6:out.append(p)
        if (v>=0)!=(u>=0):
            t=v/(v-u);out.append((p[0]+(q[0]-p[0])*t,p[1]+(q[1]-p[1])*t))
    return out
hexagon=[(math.sin(i*math.pi/3)*69.282,-math.cos(i*math.pi/3)*69.282) for i in range(6)]
planes=[[(1,0,0),(-.577333,-1,0)],[(-1,0,0),(.577333,-1,0)],[(-1,0,0),(-.577333,1,0),(1.732,-1,69.28)],[(-1,0,0),(-.577333,1,0),(-1.732,1,-69.28)],[(1,0,0),(.577333,1,0),(.577333,-1,0)],[(1,0,0),(.577333,1,0),(-.577333,1,0)]]
faces=[]
for rules in planes:
    p=hexagon
    for rule in rules:p=clip(p,*rule)
    faces.append(p)
def screen(p):return ((p[0]*scale+W/2)*S,(p[1]*scale+H/2)*S)
def render(age):
    t=age/.75;fade=1-ease(2.03,2.38,t);scan=ease(1.21,1.86,t)*head[14] if 1.21<t<2.38 else 0
    polys=[]
    for i in range(98):
        if i==head[4]:continue
        pose=records[i*9];prop=records[i*9+1];phase=ease(.08+prop[0],.5+prop[0],t);k=1-phase
        center=pose[:2]+pose[2:4]*k;angle=prop[1]*k;cs,sn=math.cos(angle),math.sin(angle);size=.94+.06*phase
        alpha=ease(prop[0]*.35,.18+prop[0]*.35,t)*float(prop[2]);lit=ease(prop[3]+16,prop[3]+150,scan) if scan else 0
        alpha=(alpha+(max(alpha,.95)-alpha)*lit)*fade
        for face,p in enumerate(faces):
            paint=records[i*9+3+face];rgb=np.minimum(240,np.floor(paint[:3]*255*(1+.4*lit)+.5)).astype(int)
            points=[screen((center[0]+(x*cs-y*sn)*size,center[1]+(x*sn+y*cs)*size)) for x,y in p]
            polys.append((float(paint[3])+float(records[i*9+2][3])*k,points,tuple(rgb)+(round(alpha*255),)))
    layer=Image.new('RGBA',(W*S,H*S));draw=ImageDraw.Draw(layer)
    for depth,p,color in sorted(polys,key=lambda p:p[0]):draw.polygon(p,fill=color)
    glow=layer.filter(ImageFilter.GaussianBlur(4*S));glow.putalpha(glow.getchannel('A').point(lambda x:round(x*.25)))
    out=Image.alpha_composite(glow,layer)
    edge=Image.new('RGBA',out.size);d=ImageDraw.Draw(edge)
    if scan>0:
        for i in range(head[5]):
            ends=records[head[6]+i*2];path=records[head[6]+i*2+1];a=ends[:2];z=ends[2:4]
            qa=max(0,min(1,(scan-path[0])/path[2]));qb=max(0,min(1,(scan-path[1])/path[2]))
            if qa+qb<=0:continue
            alpha=round(255*fade*(.92 if path[3]==0 else .78))
            if qa+qb>=1:d.line([screen(a),screen(z)],fill=(8,205,229,alpha),width=max(1,round(5.4*scale*S)))
            else:
                if qa>0:d.line([screen(a),screen(a+(z-a)*qa)],fill=(8,205,229,alpha),width=max(1,round(5.4*scale*S)))
                if qb>0:d.line([screen(z-(z-a)*qb),screen(z)],fill=(8,205,229,alpha),width=max(1,round(5.4*scale*S)))
    out=Image.alpha_composite(out,edge)
    if t>.86:
        i=head[4];pose=records[i*9];travel=1-ease(.86,1.18,t);center=pose[:2]+np.array([-90,95])*travel;size=1+.2*travel;cs,sn=math.cos(-.16*travel),math.sin(-.16*travel)
        blue=Image.new('RGBA',out.size);d=ImageDraw.Draw(blue)
        for face,p in enumerate(faces):
            rgb=tuple(np.minimum(255,np.floor(records[i*9+3+face][:3]*255+0.5)).astype(int));points=[screen((center[0]+(x*cs-y*sn)*size,center[1]+(x*sn+y*cs)*size)) for x,y in p]
            d.polygon(points,fill=rgb+(round(255*fade*ease(.86,.96,t)),))
        outline=[screen((center[0]+(x*cs-y*sn)*size,center[1]+(x*sn+y*cs)*size)) for x,y in hexagon];d.line(outline+[outline[0]],fill=(146,255,252,round(255*fade)),width=1)
        out=Image.alpha_composite(out,blue)
    return out.resize((W,H),Image.Resampling.LANCZOS)

for start in [0,64]:
    sheet=Image.new('RGBA',(W*8,H*8))
    for n in range(start,min(108,start+64)):
        frame=render(n/60);j=n-start;sheet.paste(frame,(j%8*W,j//8*H))
    sheet.save(ROOT/'assets'/f'mod-wall-{start//64}.png',optimize=True)
render(1.30).save(ROOT/'mod-study'/'wall-desktop.png')
print('Rendered 108 frames from 98 source tiles and',head[5],'authored scan edges')
