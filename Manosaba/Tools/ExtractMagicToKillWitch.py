"""Read-only conversion of the Act 1-5 Adv09 Still 20_1 reveal and camera commands."""
import argparse
import json
import shutil
import sys
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--script-bundle', type=Path, required=True)
parser.add_argument('--image', type=Path, required=True)
parser.add_argument('--unitypy-path', type=Path)
args = parser.parse_args()
if args.unitypy_path:
    sys.path.insert(0, str(args.unitypy_path))
import UnityPy

env = UnityPy.load(str(args.script_bundle))
scenario = next(o.read_typetree() for o in env.objects if o.type.name == 'MonoBehaviour'
                and o.read_typetree().get('m_Name') == 'Act01_Chapter05_Adv09')
commands = {r['rid']: r for r in scenario['references']['RefIds']}

def raw(command, field):
    return ''.join(part.get('text', '') for part in command['data'][field]['raw']['parts'])

# Retain the original commands used to derive the portable camera data.
ids = [1333, 1334, 1335, 1336, 1337, 1338, 1339, 1341, 1342, 1343, 1344, 1345, 1346]
source = [{'line': commands[i]['data']['playbackSpot']['lineIndex'],
           'type': commands[i]['type']['class'],
           'parameters': {k: raw(commands[i], k) for k, v in commands[i]['data'].items()
                          if isinstance(v, dict) and v.get('hasValue')}} for i in ids]
assert raw(commands[1333], 'AppearanceAndTransition') == '20_1'
assert raw(commands[1337], 'AppearanceAndTransition') == '20_1'
assert raw(commands[1341], 'AppearanceAndTransition') == '20_1'

def position(i):
    return [float(x) for x in raw(commands[i], 'ScenePosition').split(',')]

data = {'source': 'Act01_Chapter05_Adv09 / Stills.20_1', 'commands': source,
        'first': {'position': position(1333), 'scale': float(raw(commands[1333], 'Scale')),
                  'target_y': float(raw(commands[1334], 'ScenePositionY')),
                  'duration': float(raw(commands[1334], 'Duration'))},
        'second': {'position': position(1337), 'scale': float(raw(commands[1337], 'Scale')),
                   'target_y': float(raw(commands[1338], 'ScenePositionY')),
                   'duration': float(raw(commands[1338], 'Duration'))},
        'reveal': {'position': position(1341), 'scale': float(raw(commands[1341], 'Scale')),
                   'target_scale': float(raw(commands[1343], 'Scale')),
                   'duration': float(raw(commands[1343], 'Duration')),
                   'blur_duration': float(raw(commands[1344], 'FadeDuration')),
                   'crossfade': float(raw(commands[1345], 'Duration'))},
        'white_fade': float(raw(commands[1335], 'Duration')),
        # Measured against the supplied recording; camera values/easing remain native.
        'cuts': [1.25, 3.77, 6.28], 'duration': 11.28, 'exit_fade': 0.6}
project = Path(__file__).resolve().parents[1]
art = project / 'Manosaba/images/vfx/magic_to_kill_witch'
scenes = project / 'Manosaba/scenes/vfx/magic_to_kill_witch'
art.mkdir(parents=True, exist_ok=True)
scenes.mkdir(parents=True, exist_ok=True)
shutil.copyfile(args.image, art / 'Still_020_001.png')
(scenes / 'original_scene.json').write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print('Converted Still_020_001 and the three original Adv09 camera sequences.')
