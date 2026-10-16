#!/usr/bin/env bash
# macOS build with Apple clang. Only libSystem is linked dynamically (macOS has no static
# libc); everything else, dav1d included, is static. x86_64 cross-compiles on arm64.
#
# usage: build/ffmpeg/build-macos.sh <arm64|x86_64>
# needs: nasm meson ninja pkg-config (brew)

set -euo pipefail

arch=${1:?usage: build-macos.sh <arm64|x86_64>}
here=$(cd "$(dirname "$0")" && pwd)
. "$here/common.sh"

flags="-arch $arch -mmacosx-version-min=$MACOS_MIN_VERSION"
cross=()
cross_file=""

if [ "$arch" != "$(uname -m)" ]; then
    cross_file=$work/cross-$arch.ini
    cat > "$cross_file" <<EOF
[binaries]
c = ['clang', '-arch', '$arch', '-mmacosx-version-min=$MACOS_MIN_VERSION']
ar = 'ar'
strip = 'strip'
pkg-config = 'pkg-config'

[host_machine]
system = 'darwin'
cpu_family = '$( [ "$arch" = arm64 ] && echo aarch64 || echo x86_64 )'
cpu = '$arch'
endian = 'little'
EOF
    cross=(--enable-cross-compile --arch="$arch" --target-os=darwin)
fi

CFLAGS="$flags" LDFLAGS="$flags" build_dav1d "$cross_file"

build_ffmpeg "" \
    ${cross[@]+"${cross[@]}"} \
    --cc=clang \
    --extra-cflags="$flags" \
    --extra-ldflags="$flags"

strip "$out/ffmpeg" "$out/ffprobe"
