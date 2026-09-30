"""Read-only conversion of the original Unity cut-in into portable Godot animation data.

Requires UnityPy. No Unity executable, game injection, or game file modification.
The build/runtime does not depend on Python or UnityPy after this conversion.
"""
import argparse
import bisect
import json
import math
import struct
import sys
import zlib
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--bundle', type=Path, required=True)
parser.add_argument('--unitypy-path', type=Path)
parser.add_argument('--character', choices=['Ema', 'Hiro', 'CreatureHiro'], default='Ema')
args = parser.parse_args()
if args.unitypy_path:
    sys.path.insert(0, str(args.unitypy_path))
import UnityPy

project = Path(__file__).resolve().parents[1]
art = project / 'Manosaba/images/vfx/refute_cutin'
audio = project / 'Manosaba/audio/vfx/refute_cutin'
theme = 'ema' if args.character == 'Ema' else 'hiro'
timeline_name = {'Ema': 'original_timeline', 'Hiro': 'hiro_timeline', 'CreatureHiro': 'creature_hiro_timeline'}[args.character]
output = project / f'Manosaba/scenes/vfx/refute_cutin/{timeline_name}.json'
env = UnityPy.load(str(args.bundle))
objects = {o.path_id: o for o in env.objects}
cache = {}

def read(i):
    if i not in cache:
        cache[i] = objects[i].read_typetree()
    return cache[i]

def ptr(d):
    return d.get('m_PathID', 0)

root_id = next(o.path_id for o in env.objects if o.type.name == 'GameObject'
               and read(o.path_id)['m_Name'] == 'ObjectionCutIn_' + args.character)
nodes = {}
components = {}

def hierarchy(i, parent=None):
    go = read(i)
    cs = [ptr(c['component']) for c in go['m_Component']]
    tr = read(cs[0])
    nodes[i] = {'name': go['m_Name'], 'parent': parent, 'children': [],
                'position': [tr['m_LocalPosition'][k] for k in 'xyz'],
                'scale': [tr['m_LocalScale'][k] for k in 'xyz'],
                'angle': 0.0, 'active': bool(go['m_IsActive']),
                'alpha': 1.0, 'params': {}, 'components': cs}
    for c in cs:
        components[c] = i
    if parent is not None:
        nodes[parent]['children'].append(i)
    for child in tr['m_Children']:
        hierarchy(ptr(read(ptr(child))['m_GameObject']), i)

hierarchy(root_id)
library = next(read(c)['_objectLibrary'] for c in nodes[root_id]['components']
               if '_objectLibrary' in read(c))
expressions = {ptr(entry['Objects'][0]): index for index, entry in enumerate(library)}
director = next(read(c) for c in nodes[next(i for i in nodes if nodes[i]['name'] == 'Timeline')]['components']
                if objects[c].type.name == 'PlayableDirector')
bindings = {ptr(b['key']): components.get(ptr(b['value']), ptr(b['value']))
            for b in director['m_SceneBindings'] if ptr(b['key'])}
exposed = {key: ptr(value) for key, value in director['m_ExposedReferences']['m_References']}
controls = {}
def control_tracks(i, muted=False):
    track = read(i)
    muted = muted or bool(track.get('m_Muted'))
    if muted:
        return
    for clip in track.get('m_Clips', []):
        asset = read(ptr(clip['m_Asset']))
        if 'sourceGameObject' in asset and asset.get('updateParticle'):
            target = exposed.get(asset['sourceGameObject']['exposedName'])
            if target in nodes:
                controls[target] = {'start': clip['m_Start'], 'end': clip['m_Start']+clip['m_Duration'],
                                    'seed': asset['particleRandomSeed']}
    for child in track.get('m_Children', []):
        control_tracks(ptr(child), muted)
for track in read(ptr(director['m_PlayableAsset']))['m_Tracks']:
    control_tracks(ptr(track))
props = {zlib.crc32(s.encode()) & 0x0fffffff | 0x80000000: s
         for s in ['_Tick', '_Fader', '_Fader2', '_Float']}
clips = {}

def decode_clip(i, target):
    key = (i, target)
    if key in clips:
        return clips[key]
    d = read(i)
    packed = d['m_MuscleClip']['m_Clip']['data']
    stream = packed['m_StreamedClip']
    raw = struct.pack('<%dI' % len(stream['data']), *stream['data'])
    channels = [[] for _ in range(stream['curveCount'])]
    pos = 0
    while pos < len(raw):
        t, count = struct.unpack_from('<fi', raw, pos)
        pos += 8
        for _ in range(count):
            index, *coeff = struct.unpack_from('<i4f', raw, pos)
            pos += 20
            if math.isfinite(t):
                channels[index].append((t, coeff))
    dense = packed['m_DenseClip']
    assert dense['m_CurveCount'] == 0, 'Unexpected dense animation channels'
    for v in packed['m_ConstantClip']['data']:
        channels.append([(-1e30, [0, 0, 0, v])])
    paths = {0: target}
    def add_paths(node, path=''):
        if path:
            paths[zlib.crc32(path.encode())] = node
        for child in nodes[node]['children']:
            add_paths(child, (path + '/' if path else '') + nodes[child]['name'])
    add_paths(target)
    normalized = []
    index = 0
    for b in d['m_ClipBindingConstant']['genericBindings']:
        size = 3 if b['typeID'] == 4 and b['attribute'] in [1, 3, 4] else 1
        normalized.append((paths[b['path']], b['typeID'], b['attribute'], channels[index:index+size]))
        index += size
    assert index == len(channels), (d['m_Name'], index, len(channels))
    clips[key] = normalized
    return normalized

def sample(keys, time):
    k = keys[max(0, bisect.bisect_right([k[0] for k in keys], time) - 1)]
    dt = max(0, time - k[0]) if k[0] > -1e20 else 0.0
    a, b, c, d = k[1]
    return ((a * dt + b) * dt + c) * dt + d

tracks = []
audio_cues = []
for tid, target in bindings.items():
    tr = read(tid)
    if target not in nodes:
        continue
    infinite = ptr(tr.get('m_InfiniteClip', {}))
    if infinite:
        tracks.append((target, decode_clip(infinite, target), 0.0, 10.0,
                       tr['m_InfiniteClipOffsetPosition']))
    for c in tr.get('m_Clips', []):
        asset = read(ptr(c['m_Asset']))
        clip = ptr(asset.get('m_Clip', {}))
        if clip and objects[clip].type.name == 'AnimationClip':
            tracks.append((target, decode_clip(clip, target), c['m_Start'],
                           c['m_Duration'], None))
        elif clip and objects[clip].type.name == 'AudioClip':
            for filename, data in objects[clip].read().samples.items():
                (audio / filename).write_bytes(data)
                audio_cues.append({'file': filename, 'start': c['m_Start'], 'duration': c['m_Duration']})

sprites = []
emitter_activity = {i: [] for i, n in nodes.items()
                    if any(objects[c].type.name == 'ParticleSystem' for c in n['components'])}
emitter_frames = {i: [] for i in controls if i in emitter_activity}
for i, node in nodes.items():
    for c in node['components']:
        if objects[c].type.name != 'SpriteRenderer':
            continue
        d = read(c)
        sid = ptr(d['m_Sprite'])
        if not sid or node['name'].startswith('CutIN_Ema') or node['name'] == 'White_1':
            continue
        s = read(sid)
        texture = s['m_Name'] + '.png'
        if not (art / texture).exists():
            objects[sid].read().image.save(art / texture)
        material = read(ptr(d['m_Materials'][0]))
        node['alpha'] = d['m_Color']['a']
        node['params'] = dict(material['m_SavedProperties']['m_Floats'])
        sprites.append({'node': i, 'name': node['name'], 'texture': texture,
                        'ppu': s['m_PixelsToUnits'], 'sort': d['m_SortingOrder'],
                        'material': material['m_Name'], 'colors': dict(material['m_SavedProperties']['m_Colors']),
                        'expression': expressions.get(i, -1), 'frames': []})

for frame in range(201):
    time = frame / 60.0
    state = {i: {**n, 'position': n['position'][:], 'scale': n['scale'][:],
                 'params': n['params'].copy()} for i, n in nodes.items()}
    # The game's _objectLibrary selects one expression; activation tracks are muted.
    for i, n in state.items():
        if i in expressions:
            n['active'] = True
    for tid, target in bindings.items():
        tr = read(tid)
        if tr.get('m_Muted') or target not in state:
            continue
        if tr['m_Name'].startswith('Activation Track'):
            if target not in expressions:
                state[target]['active'] = any(c['m_Start'] <= time < c['m_Start']+c['m_Duration']
                                              for c in tr['m_Clips'])
    for target, curves, start, duration, offset in tracks:
        if time < start:
            continue
        local = max(0, min(time - start, duration))
        # Infinite clips store absolute times; regular Timeline clips store local times.
        for node, typ, attr, channels in curves:
            n = state[node]
            values = [sample(k, local) for k in channels]
            if typ == 4:
                if attr == 1:
                    n['position'] = [v + (offset[k] if offset else 0)
                                     for v, k in zip(values, 'xyz')]
                elif attr == 3:
                    n['scale'] = values
                elif attr == 4:
                    n['angle'] = values[2]
            elif typ == 1 and attr == 2086281974:
                n['active'] = values[0] > 0.5
            elif typ == 212 and attr == 304273561:
                n['alpha'] = values[0]
            elif typ == 212 and attr in props:
                n['params'][props[attr]] = values[0]
    for i, control in controls.items():
        state[i]['active'] = control['start'] <= time < control['end']
    def world(i):
        n = state[i]
        if 'world' in n:
            return n['world']
        x, y = n['position'][:2]
        sx, sy = n['scale'][:2]
        angle = n['angle']
        active = n['active']
        if n['parent'] is not None:
            px, py, psx, psy, pa, pact = world(n['parent'])
            rad = math.radians(pa)
            x, y = px + x*psx*math.cos(rad)-y*psy*math.sin(rad), py+x*psx*math.sin(rad)+y*psy*math.cos(rad)
            sx, sy, angle, active = sx*psx, sy*psy, angle+pa, active and pact
        n['world'] = (x, y, sx, sy, angle, active)
        return n['world']
    for sprite in sprites:
        n = state[sprite['node']]
        x, y, sx, sy, angle, active = world(sprite['node'])
        params = n['params']
        sprite['frames'].append([round(v, 7) for v in [x, y, sx, sy, angle, n['alpha'],
                                      int(active), params.get('_Tick', 1),
                                      params.get('_Fader', 0), params.get('_Fader2', 0),
                                      params.get('_Float', .3)]])
    for i in emitter_activity:
        if world(i)[5]:
            emitter_activity[i].append(time)
    for i in emitter_frames:
        emitter_frames[i].append([round(float(x),7) for x in world(i)])

for s in sprites:
    del s['node']
# These Shader Graph samplers read RGB, ignoring texture alpha. RGB-only copies
# prevent Godot's alpha-border importer from replacing transparent black RGB data.
glass_material = next(d for d in cache.values() if d.get('m_Name') == 'Glasses_Fix_' + ('Ema' if theme == 'ema' else 'Hiro'))
for property_name, filename in [('_kirakira', f'{theme}_cracks_rgb.png'),
                                ('_kirakira2', f'{theme}_glitter_rgb.png')]:
    texture_id = next(ptr(t['m_Texture']) for name, t in glass_material['m_SavedProperties']['m_TexEnvs']
                      if name == property_name)
    objects[texture_id].read().image.convert('RGB').save(art / filename)
particle_reader = next(objects[c] for n in nodes.values() if n['name'] == 'Glass' for c in n['components']
                       if objects[c].type.name == 'ParticleSystem')
particle = read(particle_reader.path_id)
initial = particle['InitialModule']
gradient = particle['ColorModule']['gradient']['maxGradient']
particle_renderer = next(read(c) for n in nodes.values() if n['name'] == 'Glass' for c in n['components']
                         if objects[c].type.name == 'ParticleSystemRenderer')
particle_mat = read(ptr(particle_renderer['m_Materials'][0]))
particle_texture = next(ptr(t['m_Texture']) for name, t in particle_mat['m_SavedProperties']['m_TexEnvs']
                        if name == '_BaseMap')
objects[particle_texture].read().image.save(art / 'original_kirakira.png')
particles = {'texture': 'original_kirakira.png', 'start': 80/60,
             'duration': particle['lengthInSec'], 'seed': particle['randomSeed'],
             'rate': particle['EmissionModule']['rateOverTime']['scalar'],
             'radius': particle['ShapeModule']['radius']['value'],
             'lifetime': [initial['startLifetime']['minScalar'], initial['startLifetime']['scalar']],
             'speed': [initial['startSpeed']['minScalar'], initial['startSpeed']['scalar']],
             'size': [initial['startSize']['minScalar'], initial['startSize']['scalar']],
             'alpha': [[gradient[f'atime{i}']/65535, gradient[f'key{i}']['a']]
                       for i in range(gradient['m_NumAlphaKeys'])]}

def scalar_range(value):
    return [value['minScalar'] if value['minMaxState'] == 3 else value['scalar'], value['scalar']]

def curve_samples(value):
    keys = value['maxCurve']['m_Curve']
    values = []
    for f in range(65):
        t = f/64
        if not keys:
            values.append(value['scalar'])
            continue
        if t <= keys[0]['time']:
            v = keys[0]['value']
        elif t >= keys[-1]['time']:
            v = keys[-1]['value']
        else:
            left = max(k for k in range(len(keys)) if keys[k]['time'] <= t)
            a,b = keys[left:left+2]
            duration = b['time']-a['time']
            u = (t-a['time'])/duration
            v = (2*u**3-3*u*u+1)*a['value'] + (u**3-2*u*u+u)*a['outSlope']*duration + (-2*u**3+3*u*u)*b['value'] + (u**3-u*u)*b['inSlope']*duration
        values.append(round(v*value['scalar'],7))
    return values

extra_particles = []
for i, frames in emitter_frames.items():
    node = nodes[i]
    if node['name'] == 'Glass':
        particles['seed'] = controls[i]['seed']
        continue
    particle_id = next(c for c in node['components'] if objects[c].type.name == 'ParticleSystem')
    renderer_id = next(c for c in node['components'] if objects[c].type.name == 'ParticleSystemRenderer')
    p, renderer = read(particle_id), read(renderer_id)
    mat = read(ptr(renderer['m_Materials'][0]))
    uv = p['UVModule']
    textures = []
    if uv['enabled'] and uv['mode'] == 1:
        for entry in uv['sprites']:
            sprite_id = ptr(entry['sprite'])
            sprite = read(sprite_id)
            filename = sprite['m_Name']+'.png'
            objects[sprite_id].read().image.save(art/filename)
            textures.append(filename)
    else:
        tex_id = next(ptr(value['m_Texture']) for key,value in mat['m_SavedProperties']['m_TexEnvs']
                      if key in ['_MainTex','_BaseMap'] and ptr(value['m_Texture']))
        tex = objects[tex_id].read()
        filename = tex.m_Name+'.png'
        tex.image.save(art/filename)
        textures.append(filename)
    initial = p['InitialModule']
    gradient = p['ColorModule']['gradient']['maxGradient']
    colors = [[gradient[f'ctime{k}']/65535]+[gradient[f'key{k}'][c] for c in 'rgb']
              for k in range(gradient['m_NumColorKeys'])]
    alphas = [[gradient[f'atime{k}']/65535,gradient[f'key{k}']['a']]
              for k in range(gradient['m_NumAlphaKeys'])]
    q = read(node['components'][0])['m_LocalRotation']
    direction = [2*(q['x']*q['z']+q['w']*q['y']), 2*(q['y']*q['z']-q['w']*q['x'])]
    extra_particles.append({'name': node['name'], 'textures': textures, 'frames': frames,
                            'start': controls[i]['start'], 'end': controls[i]['end'], 'seed': controls[i]['seed'],
                            'rate': p['EmissionModule']['rateOverTime']['scalar'],
                            'bursts': [[b['time'],int(b['countCurve']['scalar'])] for b in p['EmissionModule']['m_Bursts']],
                            'lifetime': scalar_range(initial['startLifetime']), 'speed': scalar_range(initial['startSpeed']),
                            'size': scalar_range(initial['startSize']), 'shape': p['ShapeModule']['type'],
                            'shape_scale': [p['ShapeModule']['m_Scale'][k] for k in 'xy'],
                            'radius': p['ShapeModule']['radius']['value'], 'direction': direction,
                            'sort': renderer['m_SortingOrder'], 'colors': colors, 'alpha': alphas,
                            'initial_color': [initial['startColor']['maxColor'][k] for k in 'rgba'],
                            'additive': dict(mat['m_SavedProperties']['m_Floats']).get('_DstBlend') == 1,
                            'sizes': curve_samples(p['SizeModule']['curve']) if p['SizeModule']['enabled'] else [1.0]*65,
                            'uv_curve': curve_samples(uv['frameOverTime']), 'cycles': uv['cycles']})
manifest = {'source': args.bundle.name, 'prefab': 'ObjectionCutIn_' + args.character, 'theme': theme,
            'native_duration': 200/60, 'sample_rate': 60, 'pixels_per_unit': 75,
            'sprites': sprites, 'particles': particles, 'extra_particles': extra_particles,
            'audio': audio_cues}
output.write_text(json.dumps(manifest, ensure_ascii=False, separators=(',', ':'), allow_nan=False), encoding='utf8')
print(f'Converted {len(sprites)} renderers, {len(clips)} clips, 201 frames to {output}')
for i, times in emitter_activity.items():
    if nodes[i]['name'] != 'Glass':
        print('Additional prefab emitter:', nodes[i]['name'], 'hierarchy active samples:', len(times))
