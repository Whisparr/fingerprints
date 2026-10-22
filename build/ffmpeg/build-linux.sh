#!/usr/bin/env bash
# Fully static musl build, run inside an Alpine container of the target architecture.
# The binary has no libc dependency, so one build serves both glibc and musl RIDs.
#
# usage: docker run --rm --platform linux/<arch> -v "$PWD:/src" -w /src alpine:3.22 \
#            sh -c 'apk add --no-cache bash && build/ffmpeg/build-linux.sh'

set -euo pipefail

apk add --no-cache build-base curl nasm pkgconf meson ninja-build python3 xz tar coreutils >/dev/null

here=$(cd "$(dirname "$0")" && pwd)
. "$here/common.sh"

build_dav1d

build_ffmpeg "" \
    --extra-ldflags="-static" \
    --extra-ldexeflags="-static"

strip "$out/ffmpeg" "$out/ffprobe"
