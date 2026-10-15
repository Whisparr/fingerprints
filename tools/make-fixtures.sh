#!/usr/bin/env bash
# Regenerates the reference vectors the C# tests compare against.
#
# All Go reference values come from a linux/amd64 build run under Docker: arm64 Go
# fuses multiply-adds (math.Cos, rgb2GrayRGBA), so stash's own hashes differ by a few
# ulps there. amd64 is the canonical platform.
#
# usage: tools/make-fixtures.sh [ffmpeg] [ffprobe]
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
ffmpeg=${1:-ffmpeg}
ffprobe=${2:-ffprobe}
out="$root/tests/Whisparr.Fingerprints.Test/Vectors"
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

(cd "$root/tools/vectors" && GOOS=linux GOARCH=amd64 CGO_ENABLED=0 go build -o "$work/vectors" .)
vectors() { docker run --rm --platform linux/amd64 -v "$work:/w" -v "$out:/out" busybox:1.37 /w/vectors "$@"; }

# The oracle: stash's own hashing code via peolic/videohashes. Built for the host;
# oshash is integer-only, and its phash is recorded for comparison, not asserted.
oracle_sha=25b54691c183c4a4a69e5a4fa9e2e14667ecf1fc
git clone -q https://github.com/peolic/videohashes.git "$work/oracle"
git -C "$work/oracle" checkout -q "$oracle_sha"
(cd "$work/oracle" && go build -o "$work/videohashes" ./cmd/videohashes)
# it only looks for ffmpeg/ffprobe in its working directory
mkdir -p "$work/oracle-run"
ln -sf "$(command -v "$ffmpeg")" "$work/oracle-run/ffmpeg"
ln -sf "$(command -v "$ffprobe")" "$work/oracle-run/ffprobe"
oracle() { (cd "$work/oracle-run" && "$work/videohashes" -json "$1"); }

mkdir -p "$out"
vectors tables > "$out/tables.json"
vectors synthetic > "$out/synthetic.json"
vectors seeks 634 10.56 1.5 3599.99 7322.43 > "$out/seeks.json"

for clip in "$out"/clips/*.mp4; do
    name=$(basename "$clip" .mp4)
    frames="$out/clips/$name"
    rm -rf "$frames" && mkdir -p "$frames"

    # stash: FileDuration = format.duration rounded to 0.01s
    duration=$("$ffprobe" -v error -show_entries format=duration -of csv=p=0 "$clip")
    duration=$(python3 -c "import sys; print(round(float(sys.argv[1]) * 100) / 100)" "$duration")

    i=0
    for t in $(vectors seeks "$duration" | python3 -c "import sys, json; print(' '.join(next(iter(json.load(sys.stdin).values()))))"); do
        "$ffmpeg" -v error -y -ss "$t" -i "$clip" -frames:v 1 -vf scale=160:-2 -c:v bmp -f rawvideo - > "$frames/$(printf %02d $i).bmp"
        i=$((i + 1))
    done

    oracle "$clip" > "$work/oracle.json"

    vectors frames "/out/clips/$name" | python3 -c "
import sys, json
d = json.load(sys.stdin)
o = json.load(open(sys.argv[2]))
d['duration'] = float(sys.argv[1])
d['oshash'] = o['oshash']
d['oracle'] = {'videohashes': sys.argv[3], **o}
json.dump(d, sys.stdout, indent=2)
" "$duration" "$work/oracle.json" "$oracle_sha" > "$out/clips/$name.json"
done

echo "vectors written to $out"
