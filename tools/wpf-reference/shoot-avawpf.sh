#!/usr/bin/env bash
set -euo pipefail

# Renders the AvaWpf side of the WPF reference scenes (spec section 13) with the gallery's headless --scene mode, for
# every family and scheme in families.json, into <out>/<scene>/<family>.<scheme>.<state>.png. report.py compares them
# with the WPF shots of run.py.

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
here="$root/tools/wpf-reference"

usage() {
    cat <<'TEXT'
shoot-avawpf.sh [OPTIONS]

  --scene NAMES    scene names, comma separated (default: every file in tools/wpf-reference/scenes)
  --family NAMES   families.json families, comma separated (default: all)
  --scheme NAMES   families.json schemes, comma separated (default: all)
  --out DIR        output folder, default artifacts/avawpf-shots
  --no-build       do not build the gallery first
  -j N             families.json entries rendered at the same time (default: half the processors)
  -h               this help
TEXT
}

scenes= families= schemes= out="$root/artifacts/avawpf-shots" build=1
jobs=$(( $(nproc 2>/dev/null || echo 2) / 2 )); [ "$jobs" -ge 1 ] || jobs=1
while [ $# -gt 0 ]; do
    case "$1" in
        --scene) scenes=${2:?}; shift 2 ;;
        --family) families=${2:?}; shift 2 ;;
        --scheme) schemes=${2:?}; shift 2 ;;
        --out) out=${2:?}; shift 2 ;;
        --no-build) build=0; shift ;;
        -j) jobs=${2:?}; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument '$1'" >&2; exit 1 ;;
    esac
done

if [ -z "$scenes" ]; then
    scene_arg="$here/scenes"
else
    scene_arg=$(printf '%s\n' "${scenes//,/$'\n'}" | sed "s|^|$here/scenes/|; s|\$|.json|" | paste -sd, -)
fi

project="$root/samples/AvaWpf.Gallery.Desktop"
if [ "$build" = 1 ]; then
    dotnet build "$project" -c Release -v q -nologo >&2
fi
dll="$project/bin/Release/net10.0/AvaWpf.Gallery.Desktop.dll"

# One line per entry: <family>.<scheme> <avawpf family> <avawpf scheme> <avawpf variant>
python3 - "$here/families.json" "$families" "$schemes" <<'PY' |
import json, sys
entries = json.load(open(sys.argv[1]))["entries"]
fams = {f.lower() for f in sys.argv[2].split(",") if f}
schs = {s.lower() for s in sys.argv[3].split(",") if s}
for e in entries:
    if fams and e["family"].lower() not in fams:
        continue
    if schs and e["scheme"].lower() not in schs:
        continue
    a = e["avawpf"]
    print(f'{e["family"]}.{e["scheme"]} {a["family"]} {a["scheme"]} {a["variant"]}')
PY
{
while read -r prefix family scheme variant; do
    while [ "$(jobs -rp | wc -l)" -ge "$jobs" ]; do
        wait -n || true
    done
    echo "[shoot-avawpf] $prefix" >&2
    dotnet "$dll" --theme "$family" --scheme "$scheme" --variant "$variant" --disable-animations \
        --scene "$scene_arg" --out "$out" --shot-prefix "$prefix" > /dev/null &
done
wait
}
