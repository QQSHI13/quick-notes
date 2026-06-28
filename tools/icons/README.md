# Icon source

**pound.svg** — Material Design pound (#) icon.
Source: https://pictogrammers.com/library/mdi/icon/pound/

To regenerate the MSIX asset set (58 PNGs at all scales/sizes):

1. Ensure `inkscape` and `python3` with Pillow are in PATH.
2. Run from the repo root:

```bash
inkscape -w 1024 -h 1024 tools/icons/pound.svg -o /tmp/pound_master.png
python3 -c \"
from PIL import Image
im = Image.open('/tmp/pound_master.png').convert('RGBA')
a = im.getchannel('A')
white = Image.new('RGBA', im.size, (255,255,255,0))
white.putalpha(a)
white.save('/tmp/pound_master_white.png')
\"
python3 << 'PY'
import os, glob
from PIL import Image

MASTER = '/tmp/pound_master_white.png'
ASSETS = 'QuickNotes/Assets'

m = Image.open(MASTER).convert('RGBA')

def save(img, name):
    img.save(os.path.join(ASSETS, name), 'PNG')

scales = {'': 1, '.scale-100': 1, '.scale-125': 1.25, '.scale-150': 1.5,
          '.scale-200': 2, '.scale-400': 4}
squares = {'StoreLogo': 50, 'Square44x44Logo': 44, 'SmallTile': 71,
           'Square150x150Logo': 150, 'LargeTile': 310}
for name, base in squares.items():
    for suf, mult in scales.items():
        sz = max(1, round(base * mult))
        save(m.resize((sz, sz), Image.LANCZOS), f'{name}{suf}.png')
wides = {'Wide310x150Logo': (310, 150), 'SplashScreen': (620, 300)}
for name, (bw, bh) in wides.items():
    for suf, mult in scales.items():
        w, h = max(1, round(bw * mult)), max(1, round(bh * mult))
        canvas = Image.new('RGBA', (w, h), (0,0,0,0))
        side = min(w, h)
        g = m.resize((side, side), Image.LANCZOS)
        canvas.alpha_composite(g, ((w-side)//2, (h-side)//2))
        save(canvas, f'{name}{suf}.png')
for suf, mult in scales.items():
    sz = max(1, round(24 * mult))
    save(m.resize((sz, sz), Image.LANCZOS), f'LockScreenLogo{suf}.png')
for n in (16, 24, 32, 48, 256):
    save(m.resize((n,n), Image.LANCZOS), f'Square44x44Logo.targetsize-{n}.png')
    save(m.resize((n,n), Image.LANCZOS), f'Square44x44Logo.altform-unplated_targetsize-{n}.png')
print('done')
PY
```
