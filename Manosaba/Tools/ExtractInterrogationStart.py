"""Convert BeginDebateText UI and its native animation curves; original files are read-only."""
import argparse
import bisect
import json
import math
import shutil
import struct
import sys
import zlib
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--bundle', type=Path, required=True)
parser.add_argument('--textures', type=Path, required=True)
parser.add_argument('--unitypy-path', type=Path)
args = parser.parse_args()
if args.unitypy_path:
    sys.path.insert(0, str(args.unitypy_path))
import UnityPy

project = Path(__file__).resolve().parents[1]
art = project / 'Manosaba/images/vfx/interrogation_start'
out = project / 'Manosaba/scenes/vfx/interrogation_start'
art.mkdir(parents=True, exist_ok=True)
out.mkdir(parents=True, exist_ok=True)
env = UnityPy.load(str(args.bundle))
objects = {o.path_id: o for o in env.objects}
cache = {}

def read(i):
    if i not in cache:
        cache[i] = objects[i].read_typetree()
    return cache[i]

def ptr(d):
    return d.get('m_PathID', 0)

nodes = []
paths = {}
components = {}

def hierarchy(i, parent=-1, path=''):
    go = read(i)
    cs = [ptr(c['component']) for c in go['m_Component']]
    tr = read(cs[0])
    index = len(nodes)
    paths[zlib.crc32(path.encode())] = index
    for c in cs:
        components[c] = index
    color = [1., 1., 1., 1.]
    alpha = 1.
    texture = ''
    constrained = False
    rotation_source_transform = 0
    rotation_offset = 0.
    material = ''
    for c in cs[1:]:
        d = read(c)
        if 'm_Sprite' in d and ptr(d['m_Sprite']):
            texture = go['m_Name'] + '.png'
            # The UI uses its original untrimmed rectangle, also present in the texture dump.
            source = args.textures / texture
            if not source.exists() and go['m_Name'] == 'Start_Book':
                source = args.textures / 'start_Book.png'
            shutil.copyfile(source, art / texture)
            color = [d['m_Color'][k] for k in 'rgba']
            if ptr(d['m_Material']):
                material = read(ptr(d['m_Material']))['m_Name']
                assert material == 'Emission', f'Unexpected UI material: {material}'
        if 'm_Alpha' in d:
            alpha = d['m_Alpha']
        if objects[c].type.name == 'RotationConstraint':
            constrained = bool(d['m_Enabled'] and d['m_Active'] and d['m_AffectRotationZ'])
            if constrained:
                assert d['m_Weight'] == 1. and len(d['m_Sources']) == 1
                source = d['m_Sources'][0]
                assert source['weight'] == 1.
                rotation_source_transform = ptr(source['sourceTransform'])
                rotation_offset = d['m_RotationOffset']['z']
    anchored = [tr['m_AnchoredPosition'][k] for k in 'xy']
    node = {'name': go['m_Name'], 'parent': parent, 'texture': texture,
            'size': [tr['m_SizeDelta'][k] for k in 'xy'],
            'pivot': [tr['m_Pivot'][k] for k in 'xy'],
            'anchor_min': [tr['m_AnchorMin'][k] for k in 'xy'],
            'anchor_max': [tr['m_AnchorMax'][k] for k in 'xy'],
            'constrained': constrained, 'material': material,
            'rotation_source_transform': rotation_source_transform, 'rotation_offset': rotation_offset,
            'initial': anchored + [tr['m_LocalScale'][k] for k in 'xy'] + [0.] + color + [alpha, float(go['m_IsActive'])],
            'frames': []}
    nodes.append(node)
    for child in tr['m_Children']:
        cid = ptr(read(ptr(child))['m_GameObject'])
        hierarchy(cid, index, (path + '/' if path else '') + read(cid)['m_Name'])

root = next(o.path_id for o in env.objects if o.type.name == 'GameObject'
            and read(o.path_id)['m_Name'] == 'BeginDebateText')
root_tr = read(ptr(read(root)['m_Component'][0]['component']))
rig = next(ptr(read(ptr(c))['m_GameObject']) for c in root_tr['m_Children']
           if read(ptr(read(ptr(c))['m_GameObject']))['m_Name'] == 'Root')
hierarchy(rig)
for node in nodes:
    source = node.pop('rotation_source_transform')
    node['rotation_source'] = components[source] if source else -1

def decode(i, target):
    d = read(i)
    packed = d['m_MuscleClip']['m_Clip']['data']
    streamed = packed['m_StreamedClip']
    raw = struct.pack('<%dI' % len(streamed['data']), *streamed['data'])
    channels = [[] for _ in range(streamed['curveCount'])]
    pos = 0
    while pos < len(raw):
        t, count = struct.unpack_from('<fi', raw, pos)
        pos += 8
        for _ in range(count):
            index, *coeff = struct.unpack_from('<i4f', raw, pos)
            pos += 20
            if math.isfinite(t):
                channels[index].append((t, coeff))
    assert packed['m_DenseClip']['m_CurveCount'] == 0
    channels += [[(-1e30, [0., 0., 0., v])] for v in packed['m_ConstantClip']['data']]
    normalized = []
    index = 0
    for b in d['m_ClipBindingConstant']['genericBindings']:
        size = 3 if b['typeID'] == 4 and b['attribute'] in [1, 3, 4] else 1
        node = target if b['path'] == 0 else paths[b['path']]
        normalized.append((node, b['typeID'], b['attribute'], channels[index:index+size]))
        index += size
    assert index == len(channels)
    return normalized

def sample(keys, t):
    key = keys[max(0, bisect.bisect_right([k[0] for k in keys], t) - 1)]
    dt = max(0, t - key[0]) if key[0] > -1e20 else 0.
    a, b, c, d = key[1]
    return ((a*dt + b)*dt + c)*dt + d

director = next(read(o.path_id) for o in env.objects if o.type.name == 'PlayableDirector'
                and ptr(read(o.path_id)['m_GameObject']) in [ptr(read(ptr(c))['m_GameObject']) for c in root_tr['m_Children']])
tracks = []
duration = 0.
for binding in director['m_SceneBindings']:
    track = read(ptr(binding['key']))
    target = components[ptr(binding['value'])]
    if ptr(track.get('m_InfiniteClip', {})):
        tracks.append((decode(ptr(track['m_InfiniteClip']), target), 0., 10.))
    for clip in track['m_Clips']:
        asset = read(ptr(clip['m_Asset']))
        tracks.append((decode(ptr(asset['m_Clip']), target), clip['m_Start'], clip['m_Duration']))
        duration = max(duration, clip['m_Start'] + clip['m_Duration'])

color_attrs = {zlib.crc32(('m_Color.'+k).encode()): 5+i for i, k in enumerate('rgba')}
position_attrs = {zlib.crc32(('m_AnchoredPosition.'+k).encode()): i for i, k in enumerate('xy')}
for frame in range(round(duration*60)+1):
    time = frame/60.
    state = [n['initial'][:] for n in nodes]
    for curves, start, length in tracks:
        if time < start:
            continue
        for node, typ, attr, channels in curves:
            values = [sample(k, min(time-start, length)) for k in channels]
            s = state[node]
            if typ == 4 and attr == 3:
                s[2:4] = values[:2]
            elif typ == 4 and attr == 4:
                s[4] = values[2]
            elif typ == 224 and attr in position_attrs:
                s[position_attrs[attr]] = values[0]
            elif typ == 114 and attr in color_attrs:
                s[color_attrs[attr]] = values[0]
            elif typ == 225 and attr == zlib.crc32(b'm_Alpha'):
                s[9] = values[0]
            elif typ == 1 and attr == zlib.crc32(b'm_IsActive'):
                s[10] = float(values[0] > .5)
            else:
                raise ValueError(f'Unhandled channel: {typ} {attr}')
    for node, s in zip(nodes, state):
        node['frames'].append([round(v, 6) for v in s])
for n in nodes:
    del n['initial']
data = {'source': 'BeginDebateText / Start_Present + Start_Dismiss',
        'fps': 60, 'duration': duration, 'nodes': nodes}
(out / 'original_animation.json').write_text(json.dumps(data, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
print(f'Converted {len(nodes)} UI nodes, {len(nodes[0]["frames"])} frames, {duration:.3f}s')
