#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<'TEXT'
build-wine-fonts.sh [OPTIONS]

Compiles Wine's Tahoma, Tahoma Bold and MS Sans Serif from their FontForge
sources (.sfd) into the .ttf files that AvaWpf.Fonts ships. It runs the same
genttf.ff script as Wine's fonts/Makefile. The .ttf files are checked in, so
the normal build never needs FontForge.

  --wine DIR   Wine source tree, default $WINE_SRC, else ../wine next to the repo
  --out DIR    output folder, default src/AvaWpf.Fonts/Assets/Fonts
  -h           this help

Needs fontforge on PATH.
TEXT
}

wine_src=${WINE_SRC:-$root/../wine}
out=$root/src/AvaWpf.Fonts/Assets/Fonts

while [ $# -gt 0 ]; do
    case "$1" in
        --wine) wine_src=${2:?--wine needs a value}; shift 2 ;;
        --out) out=${2:?--out needs a value}; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument '$1'" >&2; exit 1 ;;
    esac
done

if ! command -v fontforge >/dev/null; then
    echo "fontforge is not on PATH" >&2
    exit 1
fi

fonts="$wine_src/fonts"
if [ ! -f "$fonts/genttf.ff" ]; then
    echo "no genttf.ff in $fonts; pass --wine or set WINE_SRC" >&2
    exit 1
fi

mkdir -p "$out"
for name in tahoma tahomabd ms_sans_serif; do
    (cd "$fonts" && fontforge -lang=ff -script genttf.ff "$name.sfd" "$out/$name.ttf") 2>&1 |
        grep -v -e '^Copyright' -e '^ License' -e '^ Version' -e '^ Based on' -e '^ with many parts' -e 'Lookup subtable contains unused glyph' || true
    echo "built $out/$name.ttf"
done
