# Shared by the per-platform build scripts. Sourced, not run.
#
# Builds a minimal static ffmpeg: everything stash's phash needs to read a video and
# write one scaled BMP frame to stdout, nothing else. ffprobe is built too, for the
# oracle check in CI; only ffmpeg is packaged.

set -euo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=versions.sh
. "$here/versions.sh"

work=${WORK:-$here/work}
prefix=$work/prefix
out=${OUT:-$here/out}
jobs=$(getconf _NPROCESSORS_ONLN 2>/dev/null || sysctl -n hw.ncpu)

mkdir -p "$work" "$prefix" "$out"

# usage: fetch <url> <sha256> [dir]; prints the downloaded path
fetch() {
    local url=$1 sha=$2 dir=${3:-$work} file
    mkdir -p "$dir"
    file=$dir/$(basename "$url")

    if [ ! -f "$file" ]; then
        curl -fsSL --retry 3 -o "$file.part" "$url"
        mv "$file.part" "$file"
    fi

    if command -v sha256sum >/dev/null; then
        echo "$sha  $file" | sha256sum -c - >/dev/null
    else
        echo "$sha  $file" | shasum -a 256 -c - >/dev/null
    fi

    echo "$file"
}

# Everything phash touches: any container and codec in (demuxers, parsers, decoders,
# bsfs; libdav1d for AV1), the scale filter plus what ffmpeg inserts on its own (format
# conversion, accurate-seek trim, autorotate), and the bmp encoder + rawvideo muxer for
# `-c:v bmp -f rawvideo -`. No network, no hardware, no system autodetection.
ffmpeg_flags=(
    --disable-everything
    --disable-autodetect
    --disable-network
    --disable-doc
    --disable-debug
    --disable-avdevice
    --disable-programs
    --enable-ffmpeg
    --enable-ffprobe
    --enable-static
    --disable-shared
    --enable-pic
    --enable-avfilter
    --enable-swscale
    --enable-swresample
    --enable-protocol=file,pipe
    --enable-demuxers
    --enable-parsers
    --enable-decoders
    --enable-bsfs
    --enable-libdav1d
    --enable-encoder=bmp
    --enable-muxer=rawvideo
    --enable-filter=scale,format,null,trim,setpts,transpose,hflip,vflip,rotate,anull,aformat,atrim,aresample
)

build_dav1d() {
    local cross_file=${1:-}
    local src
    src=$(fetch "https://code.videolan.org/videolan/dav1d/-/archive/$DAV1D_VERSION/dav1d-$DAV1D_VERSION.tar.gz" "$DAV1D_SHA256")

    rm -rf "$work/dav1d" && mkdir -p "$work/dav1d"
    tar -xzf "$src" -C "$work/dav1d" --strip-components=1

    local args=(
        --prefix="$prefix"
        --libdir=lib
        --buildtype=release
        --default-library=static
        -Denable_tools=false
        -Denable_tests=false
        -Denable_examples=false
        -Denable_docs=false
    )

    if [ -n "$cross_file" ]; then
        args+=(--cross-file="$cross_file")
    fi

    meson setup "$work/dav1d/build" "$work/dav1d" "${args[@]}"
    ninja -C "$work/dav1d/build" install
}

# usage: build_ffmpeg <exe suffix> [extra configure args...]
build_ffmpeg() {
    local exe=$1
    shift

    local src
    src=$(fetch "https://ffmpeg.org/releases/ffmpeg-$FFMPEG_VERSION.tar.xz" "$FFMPEG_SHA256")

    rm -rf "$work/ffmpeg" && mkdir -p "$work/ffmpeg"
    tar -xJf "$src" -C "$work/ffmpeg" --strip-components=1

    (
        cd "$work/ffmpeg"
        PKG_CONFIG_PATH="$prefix/lib/pkgconfig" ./configure \
            "${ffmpeg_flags[@]}" \
            --pkg-config=pkg-config \
            --pkg-config-flags=--static \
            --extra-cflags="-I$prefix/include" \
            --extra-ldflags="-L$prefix/lib" \
            "$@" || { tail -50 ffbuild/config.log; exit 1; }

        make -j"$jobs"
    )

    cp "$work/ffmpeg/ffmpeg$exe" "$work/ffmpeg/ffprobe$exe" "$out/"
}
