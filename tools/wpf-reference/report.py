#!/usr/bin/env python3
"""WPF vs AvaWpf comparison report (spec section 13).

Builds one static HTML page per family: WPF | AvaWpf | diff heatmap, with the percentage of differing pixels per shot
(CIE76 delta E above the threshold, 2.3 by default), and the hand triage from triage.toml.

Inputs (argument, then environment, then default):
  --wpf       the WPF shots (default: $AVAWPF_ROOT/tools/wpf-reference/out, written by run.py)
  --avawpf    the AvaWpf shots (default: $AVAWPF_ROOT/artifacts/avawpf-shots, written by the gallery's --scene)
  --out       the report folder (default: $AVAWPF_ROOT/artifacts/wpf-reference-report)
  --triage    the triage file (default: tools/wpf-reference/triage.toml)
  --root      AVAWPF_ROOT (default: two levels above this script)

Both shot trees use <scene>/<family>.<scheme>.<state>.png. Needs Pillow and numpy.
"""

from __future__ import annotations

import argparse
import fnmatch
import html
import os
import shutil
import sys
import tomllib
from dataclasses import dataclass, field
from pathlib import Path

try:
    import numpy as np
    from PIL import Image
except ImportError:
    sys.exit("report.py needs Pillow and numpy: pip install --user pillow numpy")

HERE = Path(__file__).resolve().parent


@dataclass
class Shot:
    scene: str
    family: str
    scheme: str
    state: str
    suffix: str  # "" or "@1.5x"
    wpf: Path
    avawpf: Path | None = None
    percent: float | None = None
    size_note: str = ""
    status: str = "untriaged"
    reason: str = ""
    images: dict[str, str] = field(default_factory=dict)

    @property
    def file_name(self) -> str:
        return f"{self.family}.{self.scheme}.{self.state}{self.suffix}.png"


def parse_name(name: str) -> tuple[str, str, str, str] | None:
    """'Aero2.Default.hover@1.5x.png' -> ('Aero2', 'Default', 'hover', '@1.5x')."""
    if not name.endswith(".png"):
        return None
    stem = name[:-4]
    suffix = ""
    if "@" in stem:
        stem, s = stem.split("@", 1)
        suffix = "@" + s
    parts = stem.split(".")
    if len(parts) != 3:
        return None
    return parts[0], parts[1], parts[2], suffix


def collect(wpf_dir: Path, ava_dir: Path, families: set[str]) -> list[Shot]:
    shots = []
    for scene_dir in sorted(p for p in wpf_dir.iterdir() if p.is_dir() and not p.name.startswith("_")):
        for png in sorted(scene_dir.glob("*.png")):
            parsed = parse_name(png.name)
            if not parsed:
                continue
            family, scheme, state, suffix = parsed
            if families and family.lower() not in families:
                continue
            ava = ava_dir / scene_dir.name / png.name
            shots.append(Shot(scene_dir.name, family, scheme, state, suffix, png, ava if ava.exists() else None))
    return shots


# --------------------------------------------------------------------------------------------------------------------
# Color difference


def load_rgb(path: Path) -> np.ndarray:
    """RGB float array in 0..1, transparent areas composited over white."""
    im = Image.open(path).convert("RGBA")
    a = np.asarray(im, dtype=np.float64) / 255.0
    rgb, alpha = a[..., :3], a[..., 3:4]
    return rgb * alpha + (1 - alpha)


def srgb_to_lab(rgb: np.ndarray) -> np.ndarray:
    lin = np.where(rgb <= 0.04045, rgb / 12.92, ((rgb + 0.055) / 1.055) ** 2.4)
    m = np.array([[0.4124564, 0.3575761, 0.1804375],
                  [0.2126729, 0.7151522, 0.0721750],
                  [0.0193339, 0.1191920, 0.9503041]])
    xyz = lin @ m.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > (6 / 29) ** 3, np.cbrt(xyz), xyz / (3 * (6 / 29) ** 2) + 4 / 29)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], axis=-1)


def compare(wpf: Path, ava: Path, threshold: float) -> tuple[float, np.ndarray, str]:
    """Percentage of differing pixels, the delta E map (on the union canvas) and a size note."""
    a, b = load_rgb(wpf), load_rgb(ava)
    h, w = max(a.shape[0], b.shape[0]), max(a.shape[1], b.shape[1])
    note = "" if a.shape == b.shape else f"size differs: WPF {a.shape[1]}x{a.shape[0]}, AvaWpf {b.shape[1]}x{b.shape[0]}"
    de = np.full((h, w), 100.0)  # pixels only one side has count as different
    ha, wa = min(a.shape[0], b.shape[0]), min(a.shape[1], b.shape[1])
    de[:ha, :wa] = np.linalg.norm(srgb_to_lab(a[:ha, :wa]) - srgb_to_lab(b[:ha, :wa]), axis=-1)
    percent = float((de > threshold).mean() * 100)
    return percent, de, note


def heatmap(wpf: Path, de: np.ndarray, threshold: float, dst: Path) -> None:
    """Faded WPF shot with the differing pixels in red (brighter = larger delta E)."""
    base = load_rgb(wpf)
    h, w = de.shape
    canvas = np.ones((h, w, 3))
    canvas[:base.shape[0], :base.shape[1]] = base
    gray = canvas.mean(axis=-1, keepdims=True) * 0.35 + 0.65
    out = np.repeat(gray, 3, axis=-1)
    mask = de > threshold
    strength = np.clip(de / 20.0, 0.35, 1.0)[mask]
    out[mask] = np.stack([np.ones_like(strength), 1 - strength, 1 - strength], axis=-1)
    Image.fromarray((out * 255).round().astype(np.uint8), "RGB").save(dst)


# --------------------------------------------------------------------------------------------------------------------
# Triage


def load_triage(path: Path) -> tuple[list[dict], list[str]]:
    if not path.exists():
        return [], []
    data = tomllib.loads(path.read_text(encoding="utf-8"))
    return data.get("diff", []), data.get("accepted_reasons", {}).get("reasons", [])


def triage(shot: Shot, entries: list[dict], accepted_reasons: list[str]) -> None:
    if shot.avawpf is None:
        shot.status = "not rendered"
        return
    if shot.percent == 0:
        shot.status = "identical"
        return
    for e in entries:
        if all(fnmatch.fnmatchcase(getattr(shot, k), str(e.get(k, "*"))) for k in ("scene", "family", "scheme", "state")):
            status, reason = e.get("status", "bug"), e.get("reason", "")
            if status == "accepted" and reason not in accepted_reasons:
                status, reason = "bug", f"{reason} (not an accepted reason)"
            limit = e.get("max_percent")
            if status == "accepted" and limit is not None and shot.percent is not None and shot.percent > float(limit):
                status, reason = "regression", f"{reason}: {shot.percent:.2f}% > {limit}%"
            shot.status, shot.reason = status, reason
            return


# --------------------------------------------------------------------------------------------------------------------
# HTML

CSS = """
:root { --bg: #ffffff; --fg: #1b1b1b; --muted: #666; --line: #ddd; --cell: #f6f6f6;
        --ok: #1a7f37; --bad: #c62828; --warn: #b26a00; }
@media (prefers-color-scheme: dark) {
  :root { --bg: #161616; --fg: #e8e8e8; --muted: #9a9a9a; --line: #333; --cell: #202020;
          --ok: #4caf50; --bad: #ef5350; --warn: #ffb74d; }
}
body { background: var(--bg); color: var(--fg); font: 14px/1.4 system-ui, sans-serif; margin: 16px; }
h1 { font-size: 20px; } h2 { font-size: 16px; margin-top: 28px; border-bottom: 1px solid var(--line); }
table { border-collapse: collapse; } td, th { border: 1px solid var(--line); padding: 6px; vertical-align: top; }
th { text-align: left; background: var(--cell); }
img { image-rendering: pixelated; display: block; }
.missing { color: var(--muted); font-style: italic; padding: 12px; }
.identical, .accepted { color: var(--ok); } .bug, .regression { color: var(--bad); font-weight: 600; }
.untriaged { color: var(--warn); } .muted { color: var(--muted); }
a { color: inherit; }
"""


def img_tag(rel: str, path: Path, zoom: int) -> str:
    with Image.open(path) as im:
        w, h = im.size
    return f'<img src="{html.escape(rel)}" width="{w * zoom}" height="{h * zoom}" alt="">'


def write_family_page(out: Path, family: str, shots: list[Shot], zoom: int, threshold: float) -> None:
    rows = [f"<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width'>",
            f"<title>{html.escape(family)} vs WPF</title><style>{CSS}</style></head><body>",
            f"<p><a href='index.html'>All families</a></p><h1>{html.escape(family)}: WPF | AvaWpf | diff</h1>",
            f"<p class='muted'>Differing pixel: CIE76 delta E &gt; {threshold}. Images at {zoom}x.</p>"]
    scenes: dict[str, list[Shot]] = {}
    for s in shots:
        scenes.setdefault(s.scene, []).append(s)
    for scene, items in scenes.items():
        rows.append(f"<h2>{html.escape(scene)}</h2><table><tr><th>shot</th><th>WPF</th><th>AvaWpf</th>"
                    "<th>diff</th><th>% differing</th><th>triage</th></tr>")
        for s in items:
            wpf = img_tag(s.images["wpf"], out / s.images["wpf"], zoom)
            ava = img_tag(s.images["ava"], out / s.images["ava"], zoom) if "ava" in s.images else "<div class='missing'>not rendered</div>"
            diff = img_tag(s.images["diff"], out / s.images["diff"], zoom) if "diff" in s.images else ""
            pct = "" if s.percent is None else f"{s.percent:.2f}%"
            note = f"<br><span class='muted'>{html.escape(s.size_note)}</span>" if s.size_note else ""
            reason = f"<br><span class='muted'>{html.escape(s.reason)}</span>" if s.reason else ""
            rows.append(f"<tr><td>{html.escape(s.scheme)}<br>{html.escape(s.state + s.suffix)}</td><td>{wpf}</td>"
                        f"<td>{ava}</td><td>{diff}</td><td>{pct}{note}</td>"
                        f"<td class='{s.status.replace(' ', '-')}'>{html.escape(s.status)}{reason}</td></tr>")
        rows.append("</table>")
    rows.append("</body></html>")
    (out / f"{family}.html").write_text("\n".join(rows), encoding="utf-8")


def write_index(out: Path, by_family: dict[str, list[Shot]]) -> None:
    rows = [f"<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width'>",
            f"<title>WPF Reference Report</title><style>{CSS}</style></head><body><h1>WPF reference report</h1>",
            "<table><tr><th>family</th><th>shots</th><th>rendered by AvaWpf</th><th>mean % differing</th>"
            "<th>bugs</th><th>untriaged</th></tr>"]
    for family, shots in by_family.items():
        done = [s for s in shots if s.percent is not None]
        mean = f"{sum(s.percent for s in done) / len(done):.2f}%" if done else ""
        bugs = sum(s.status in ("bug", "regression") for s in shots)
        untriaged = sum(s.status == "untriaged" for s in shots)
        rows.append(f"<tr><td><a href='{html.escape(family)}.html'>{html.escape(family)}</a></td><td>{len(shots)}</td>"
                    f"<td>{len(done)}</td><td>{mean}</td><td>{bugs}</td><td>{untriaged}</td></tr>")
    rows.append("</table></body></html>")
    (out / "index.html").write_text("\n".join(rows), encoding="utf-8")


def write_summary(out: Path, shots: list[Shot]) -> None:
    """summary.tsv: one line per shot, for scripts (sort by the percentage to find the worst shots)."""
    lines = ["scene\tfamily\tscheme\tstate\tpercent\tstatus\treason"]
    for s in shots:
        pct = "" if s.percent is None else f"{s.percent:.2f}"
        lines.append(f"{s.scene}\t{s.family}\t{s.scheme}\t{s.state}{s.suffix}\t{pct}\t{s.status}\t{s.reason}")
    (out / "summary.tsv").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", help="repository root (AVAWPF_ROOT)")
    ap.add_argument("--wpf", help="WPF shots folder")
    ap.add_argument("--avawpf", help="AvaWpf shots folder")
    ap.add_argument("--out", help="report folder")
    ap.add_argument("--triage", help="triage file")
    ap.add_argument("--family", action="append", help="only these families (repeatable or comma separated)")
    ap.add_argument("--threshold", type=float, default=2.3, help="delta E above which a pixel differs (default 2.3)")
    ap.add_argument("--zoom", type=int, default=2, help="display zoom (default 2)")
    args = ap.parse_args()

    root = Path(args.root or os.environ.get("AVAWPF_ROOT") or HERE.parent.parent).resolve()
    wpf_dir = Path(args.wpf).resolve() if args.wpf else root / "tools" / "wpf-reference" / "out"
    ava_dir = Path(args.avawpf).resolve() if args.avawpf else root / "artifacts" / "avawpf-shots"
    out = Path(args.out).resolve() if args.out else root / "artifacts" / "wpf-reference-report"
    triage_path = Path(args.triage).resolve() if args.triage else HERE / "triage.toml"
    families = {f.lower() for item in (args.family or []) for f in item.split(",") if f}

    if not wpf_dir.is_dir():
        sys.exit(f"{wpf_dir} not found; run run.py first")
    shots = collect(wpf_dir, ava_dir, families)
    if not shots:
        sys.exit(f"no WPF shots in {wpf_dir}")
    entries, accepted = load_triage(triage_path)

    shutil.rmtree(out / "img", ignore_errors=True)
    for s in shots:
        rel = Path("img") / s.scene
        (out / rel).mkdir(parents=True, exist_ok=True)
        stem = s.file_name[:-4]
        shutil.copyfile(s.wpf, out / rel / f"{stem}.wpf.png")
        s.images["wpf"] = str(rel / f"{stem}.wpf.png")
        if s.avawpf:
            shutil.copyfile(s.avawpf, out / rel / f"{stem}.avawpf.png")
            s.images["ava"] = str(rel / f"{stem}.avawpf.png")
            s.percent, de, s.size_note = compare(s.wpf, s.avawpf, args.threshold)
            heatmap(s.wpf, de, args.threshold, out / rel / f"{stem}.diff.png")
            s.images["diff"] = str(rel / f"{stem}.diff.png")
        triage(s, entries, accepted)

    by_family: dict[str, list[Shot]] = {}
    for s in shots:
        by_family.setdefault(s.family, []).append(s)
    for family, items in by_family.items():
        write_family_page(out, family, items, args.zoom, args.threshold)
    write_index(out, by_family)
    write_summary(out, shots)
    rendered = sum(s.avawpf is not None for s in shots)
    for family, items in by_family.items():
        done = [s.percent for s in items if s.percent is not None]
        if done:
            print(f"[report.py] {family}: {len(done)} shots, mean {sum(done) / len(done):.2f}% differing")
    print(f"[report.py] {len(shots)} WPF shots, {rendered} with AvaWpf shots; report: {out / 'index.html'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
