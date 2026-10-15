#!/usr/bin/env bash
# FreeBSD cross-build from Linux: clang + lld against the base system's headers and
# static libraries. Built for the oldest supported release so it runs on newer ones.
#
# usage: build/ffmpeg/build-freebsd.sh <x86_64|aarch64>
# needs: curl clang lld llvm nasm meson ninja pkg-config xz (apt)

set -euo pipefail

arch=${1:?usage: build-freebsd.sh <x86_64|aarch64>}
here=$(cd "$(dirname "$0")" && pwd)
. "$here/common.sh"

case "$arch" in
    x86_64) dist=amd64; sha=$FREEBSD_BASE_SHA256_AMD64; ffarch=x86_64 ;;
    aarch64) dist=arm64/aarch64; sha=$FREEBSD_BASE_SHA256_ARM64; ffarch=aarch64 ;;
    *) echo "unknown arch $arch" >&2; exit 1 ;;
esac

target=$arch-unknown-freebsd$FREEBSD_VERSION
sysroot=$work/sysroot-$arch

if [ ! -d "$sysroot/usr/include" ]; then
    # base.txz is named the same for every arch; keep each in its own directory
    base=$(fetch "https://download.freebsd.org/releases/$dist/$FREEBSD_VERSION-RELEASE/base.txz" "$sha" "$work/base-$arch")
    rm -rf "$sysroot" && mkdir -p "$sysroot"
    tar -xJf "$base" -C "$sysroot" ./usr/include ./usr/lib ./lib
fi

cc="clang --target=$target --sysroot=$sysroot -fuse-ld=lld"

cross_file=$work/cross-$target.ini
cat > "$cross_file" <<EOF
[binaries]
c = ['clang', '--target=$target', '--sysroot=$sysroot', '-fuse-ld=lld']
ar = 'llvm-ar'
strip = 'llvm-strip'
pkg-config = 'pkg-config'

[host_machine]
system = 'freebsd'
cpu_family = '$arch'
cpu = '$arch'
endian = 'little'
EOF

build_dav1d "$cross_file"

build_ffmpeg "" \
    --enable-cross-compile \
    --target-os=freebsd \
    --arch="$ffarch" \
    --cc="$cc" \
    --ld="$cc" \
    --ar=llvm-ar \
    --nm=llvm-nm \
    --ranlib=llvm-ranlib \
    --strip=llvm-strip \
    --extra-ldflags="-static" \
    --extra-ldexeflags="-static"

llvm-strip "$out/ffmpeg" "$out/ffprobe"
