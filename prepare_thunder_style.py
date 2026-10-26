"""Bake the native lightning texture into the locally installed Thunder portrait palette."""
from pathlib import Path
import hashlib,json
from PIL import Image
import numpy as np

ROOT=Path(__file__).resolve().parent
records=[]
for source,target in [(f'bolt-{n}.png',f'fx-thunder-bolt-{n}.png') for n in ['01b','03','04']]+[('fx-flare.png','fx-thunder-flare.png')]:
    path=ROOT/'assets'/source
    image=np.asarray(Image.open(path).convert('RGBA')).copy()
    # Preserve the original painted contour and alpha; map blue values to ochre/gold/pale planes.
    light=image[:,:,:3].max(axis=2)/255
    for channel,values in enumerate([[171,255,255],[109,196,245],[26,13,117]]):
        image[:,:,channel]=np.interp(light,[0,.65,1],values).astype('uint8')
    Image.fromarray(image).save(ROOT/'assets'/target)
    records.append({'source':source,'source_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'output':target,'output_sha256':hashlib.sha256((ROOT/'assets'/target).read_bytes()).hexdigest()})
(ROOT/'assets/thunder-style-sources.json').write_text(json.dumps({'reference':'Original local images/packed/card_portraits/defect/thunder.png','adaptation':'Original lightning/flare texture contour and alpha retained; baked ochre/yellow/pale portrait colours for WPF. New asymmetric columns drawn in code.','textures':records},ensure_ascii=False,indent=2),encoding='utf8')
print('Prepared three native gold lightning textures and one impact flare')
