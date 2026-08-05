#!/usr/bin/env python3
"""Terminal cast: left = ctl command + live colored JSON response, right = out/*.json files stacking up.
All JSON values are REAL responses captured from the live Unity IPC session."""
import os, re, json
from PIL import Image, ImageDraw, ImageFont

SCR = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(SCR, "cast_frames")
os.makedirs(OUT, exist_ok=True)
for f in os.listdir(OUT):
    os.remove(os.path.join(OUT, f))

CW, CH = 1280, 720
TERM_W = 812
PAD = 22
FS = 19
font = ImageFont.truetype("/System/Library/Fonts/Menlo.ttc", FS)
small = ImageFont.truetype("/System/Library/Fonts/SFNSMono.ttf", 15)
big = ImageFont.truetype("/System/Library/Fonts/SFNSMono.ttf", 17)
line_h = FS + 9

BG      = (13, 17, 23)
PANEL   = (22, 27, 34)
DIV     = (48, 54, 61)
PROMPT  = (126, 231, 135)
CMD     = (230, 237, 243)
KEY     = (121, 192, 255)   # cyan-blue
STR     = (143, 209, 132)   # green
NUM     = (240, 183, 120)   # orange
PUNC    = (139, 148, 158)   # grey
WHITE   = (230, 237, 243)
OKC     = (63, 185, 80)
DIM     = (110, 118, 129)
HL      = (56, 211, 100)

# ---- real captured steps (values are verbatim from live IPC responses) ----
STEPS = [
    {"cmd": "unityctl status",
     "json": {"success": True, "message": "Ready", "state": "Ready",
              "transport": "ipc", "unityVersion": "6000.3.16f1"},
     "file": ("status.json", "840 B")},
    {"cmd": "unityctl scene hierarchy",
     "json": {"success": True, "message": "Scene hierarchy captured", "rootCount": 10},
     "file": ("hierarchy.json", "5.6 KB")},
    {"cmd": "unityctl mesh create-primitive --type Cube",
     "json": {"success": True, "message": "Created Cube primitive 'cast_cube'",
              "type": "Cube", "sceneDirty": True},
     "file": ("create.json", "814 B")},
    {"cmd": "unityctl gameobject find --component MeshRenderer",
     "json": {"success": True, "message": "Found 9 GameObject(s)"},
     "file": ("find.json", "4.5 KB")},
    {"cmd": "unityctl screenshot capture --view game",
     "json": {"success": True, "message": "Screenshot captured",
              "width": 400, "height": 400, "format": "png"},
     "file": ("screenshot.json", "54 KB")},
]

def json_lines(obj):
    return json.dumps(obj, indent=2, ensure_ascii=False).split("\n")

def color_segments(line):
    """Return list of (text, color) for a pretty-printed JSON line."""
    m = re.match(r'^(\s*)("(?:[^"\\]|\\.)*")(\s*:\s*)(.*)$', line)
    if m:
        indent, key, colon, rest = m.groups()
        segs = [(indent, PUNC), (key, KEY), (colon, PUNC)]
        segs += value_segments(rest)
        return segs
    return [(line, PUNC)]

def value_segments(rest):
    trail = ""
    if rest.endswith(","):
        rest, trail = rest[:-1], ","
    if rest.startswith('"'):
        col = STR
    elif rest in ("true", "false", "null") or re.match(r'^-?\d', rest):
        col = NUM
    else:
        col = PUNC
    out = [(rest, col)]
    if trail:
        out.append((trail, PUNC))
    return out

# terminal render model: list of ("cmd", text) or ("json", segments)
history = []
files = []           # revealed files [(name,size)]
frame_no = 0

def draw(typing=None, cursor=True, pulse=0):
    global frame_no
    c = Image.new("RGB", (CW, CH), BG)
    d = ImageDraw.Draw(c)

    # ---- right file panel ----
    d.rectangle([(TERM_W, 0), (CW, CH)], fill=PANEL)
    d.line([(TERM_W, 0), (TERM_W, CH)], fill=DIV, width=2)
    fx = TERM_W + 24
    d.text((fx, 22), "out/", font=big, fill=WHITE)
    d.text((fx + 60, 25), f"{len(files)} file(s)", font=small, fill=DIM)
    d.line([(fx, 52), (CW - 24, 52)], fill=DIV, width=1)
    fy = 74
    for i, (name, size) in enumerate(files):
        newest = (i == len(files) - 1)
        rowc = HL if (newest and pulse) else WHITE
        # file icon
        d.rectangle([(fx, fy + 3), (fx + 13, fy + 19)], outline=rowc, width=2)
        d.line([(fx + 9, fy + 3), (fx + 13, fy + 7)], fill=rowc, width=2)
        d.text((fx + 24, fy), name, font=small, fill=rowc)
        d.text((CW - 24 - d.textlength(size, font=small), fy), size, font=small, fill=DIM)
        if newest and pulse:
            d.rectangle([(fx - 10, fy - 2), (fx - 7, fy + 20)], fill=HL)
        fy += 34
    # footer
    d.line([(fx, CH - 52), (CW - 24, CH - 52)], fill=DIV, width=1)
    d.ellipse([(fx, CH - 36), (fx + 10, CH - 26)], fill=OKC)
    d.text((fx + 18, CH - 38), "IPC live  transport: ipc", font=small, fill=DIM)

    # ---- left terminal ----
    d.text((PAD, 16), "unityctl  —  live Unity session", font=small, fill=PROMPT)
    d.line([(PAD, 40), (TERM_W - 20, 40)], fill=DIV, width=1)

    lines = []
    for kind, payload in history:
        lines.append((kind, payload))
    if typing is not None:
        lines.append(("typing", typing))

    top = 54
    max_lines = (CH - top - PAD) // line_h
    lines = lines[-max_lines:]
    y = top
    for kind, payload in lines:
        if kind == "cmd":
            d.text((PAD, y), "$", font=font, fill=PROMPT)
            d.text((PAD + d.textlength("$ ", font=font), y), payload, font=font, fill=CMD)
        elif kind == "typing":
            d.text((PAD, y), "$", font=font, fill=PROMPT)
            x0 = PAD + d.textlength("$ ", font=font)
            d.text((x0, y), payload, font=font, fill=CMD)
            if cursor:
                cx = x0 + d.textlength(payload, font=font) + 2
                d.rectangle([(cx, y + 3), (cx + FS * 0.5, y + line_h - 4)], fill=CMD)
        elif kind == "json":
            x = PAD + 8
            for text, col in payload:
                d.text((x, y), text, font=font, fill=col)
                x += d.textlength(text, font=font)
        y += line_h

    c.save(os.path.join(OUT, f"c{frame_no:04d}.png"))
    frame_no += 1

# ---- intro ----
for i in range(12):
    draw(typing="", cursor=(i // 4) % 2 == 0)

for step in STEPS:
    cmd = step["cmd"]
    # typing
    i = 3
    while i < len(cmd):
        draw(typing=cmd[:i], cursor=True)
        i += 3
    draw(typing=cmd, cursor=True)
    draw(typing=cmd, cursor=False)
    # commit command
    history.append(("cmd", cmd))
    # stream JSON lines
    segs = [color_segments(l) for l in json_lines(step["json"])]
    for k, s in enumerate(segs):
        history.append(("json", s))
        draw()  # reveal one line per frame
    # reveal file with pulse
    files.append(step["file"])
    for p in range(6):
        draw(pulse=1 if p % 2 == 0 else 0)
    # small gap: blank line
    history.append(("json", [("", PUNC)]))
    for _ in range(2):
        draw()

# ---- outro ----
for i in range(16):
    draw(cursor=(i // 5) % 2 == 0, pulse=1 if (i // 4) % 2 == 0 else 0)

print(f"wrote {frame_no} frames")
