#!/usr/bin/env bash
# Windows cross-build from Linux with llvm-mingw (UCRT). Statically linked, so the exe
# needs nothing beyond the system's UCRT (present on Windows 10+).
#
# usage: build/ffmpeg/build-windows.sh <x86_64|i686|aarch64>
# needs: curl nasm meson ninja pkg-config xz (apt)

set -euo pipefail

arch=${1:?usage: build-windows.sh <x86_64|i686|aarch64>}
here=$(cd "$(dirname "$0")" && pwd)
. "$here/common.sh"

triplet=$arch-w64-mingw32

toolchain=$work/llvm-mingw
if [ ! -x "$toolchain/bin/$triplet-clang" ]; then
    tarball=$(fetch "https://github.com/mstorsjo/llvm-mingw/releases/download/$LLVM_MINGW_VERSION/llvm-mingw-$LLVM_MINGW_VERSION-ucrt-ubuntu-22.04-x86_64.tar.xz" "$LLVM_MINGW_SHA256")
    rm -rf "$toolchain" && mkdir -p "$toolchain"
    tar -xJf "$tarball" -C "$toolchain" --strip-components=1
fi
export PATH=$toolchain/bin:$PATH

case "$arch" in
    x86_64) cpu_family=x86_64; ffarch=x86_64 ;;
    i686) cpu_family=x86; ffarch=x86 ;;
    aarch64) cpu_family=aarch64; ffarch=aarch64 ;;
    *) echo "unknown arch $arch" >&2; exit 1 ;;
esac

cross_file=$work/cross-$triplet.ini
cat > "$cross_file" <<EOF
[binaries]
c = '$triplet-clang'
ar = '$triplet-ar'
strip = '$triplet-strip'
windres = '$triplet-windres'
pkg-config = 'pkg-config'

[host_machine]
system = 'windows'
cpu_family = '$cpu_family'
cpu = '$arch'
endian = 'little'
EOF

build_dav1d "$cross_file"

build_ffmpeg ".exe" \
    --enable-cross-compile \
    --target-os=mingw32 \
    --arch="$ffarch" \
    --cross-prefix="$triplet-" \
    --cc="$triplet-clang" \
    --extra-ldflags="-static" \
    --extra-ldexeflags="-static"

"$triplet-strip" "$out/ffmpeg.exe" "$out/ffprobe.exe"
