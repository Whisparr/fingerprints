#!/usr/bin/env bash
# Checks the C# phash against stash's own code (peolic/videohashes) using one ffmpeg
# build for both, so any mismatch is the port, not ffmpeg.
#
# usage: tools/oracle-check.sh <ffmpeg> <ffprobe>
#
# On arm64 the Go oracle fuses multiply-adds and can differ from the (amd64-exact) port
# in the lowest bits; ORACLE_MAX_DISTANCE allows a Hamming distance there.
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
ffmpeg=$(cd "$(dirname "$1")" && pwd)/$(basename "$1")
ffprobe=$(cd "$(dirname "$2")" && pwd)/$(basename "$2")
clip=$root/tests/Whisparr.Fingerprints.Test/Vectors/clips/h264_sample.mp4
max_distance=${ORACLE_MAX_DISTANCE:-0}
oracle_sha=25b54691c183c4a4a69e5a4fa9e2e14667ecf1fc

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

exe=""
py=$(command -v python3 || command -v python)
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) exe=".exe" ;; esac

git clone -q https://github.com/peolic/videohashes.git "$work/oracle"
git -C "$work/oracle" checkout -q "$oracle_sha"
(cd "$work/oracle" && go build -o "$work/videohashes$exe" ./cmd/videohashes)

# videohashes only looks for ffmpeg/ffprobe in its working directory
mkdir -p "$work/run"
cp "$ffmpeg" "$work/run/ffmpeg$exe"
cp "$ffprobe" "$work/run/ffprobe$exe"
(cd "$work/run" && "$work/videohashes$exe" -json "$clip") > "$work/oracle.json"
cat "$work/oracle.json"

oracle_phash=$("$py" -c "import json,sys; print(json.load(open(sys.argv[1]))['phash'])" "$work/oracle.json")
oracle_oshash=$("$py" -c "import json,sys; print(json.load(open(sys.argv[1]))['oshash'])" "$work/oracle.json")
fixture_oshash=$("$py" -c "import json,sys; print(json.load(open(sys.argv[1]))['oshash'])" "${clip%.mp4}.json")

if [ "$oracle_oshash" != "$fixture_oshash" ]; then
    echo "oshash mismatch: oracle $oracle_oshash, fixture $fixture_oshash" >&2
    exit 1
fi

# Hash with the port, then compare by Hamming distance
FINGERPRINTS_FFMPEG=$ffmpeg FINGERPRINTS_EXPECTED_PHASH=$oracle_phash \
    dotnet test "$root/Fingerprints.slnx" -c Release --filter "TestCategory=IntegrationTest&Name!=sample_clip_matches_oracle" -nologo -v q

port_phash=$(FINGERPRINTS_FFMPEG=$ffmpeg dotnet run --project "$root/tools/phash" -c Release -- "$clip")

distance=$("$py" -c "import sys; print(bin(int(sys.argv[1], 16) ^ int(sys.argv[2], 16)).count('1'))" "$oracle_phash" "$port_phash")
echo "oracle $oracle_phash, port $port_phash, distance $distance (max $max_distance)"

if [ "$distance" -gt "$max_distance" ]; then
    exit 1
fi
