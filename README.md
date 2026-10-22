# Whisparr.Fingerprints

Video fingerprints for .NET that match the ones [stash](https://github.com/stashapp/stash)
and StashDB store, so a file can be looked up by its content instead of its name or folder:

- **OSHASH**: the OpenSubtitles hash (file size plus checksums of the first and last 64 KiB).
  Pure .NET, no dependencies.
- **PHASH**: stash's perceptual video hash (25 frames tiled 5×5, then a DCT hash). Needs
  ffmpeg, which the package ships as a static build for every supported runtime.

```xml
<PackageReference Include="Whisparr.Fingerprints" Version="x.y.z" />
```

```csharp
var oshash = OsHash.Compute(path);

// duration: ffprobe format.duration rounded to 0.01 s, the way stash reads it
var phash = await PerceptualHash.ComputeAsync(path, duration);
```

Both return the strings stash and StashDB use: OSHASH as 16 zero-padded hex digits, PHASH
as lowercase hex **without** padding.

## Install

Tagged releases are published to the Whisparr NuGet feed, `https://nuget.whisparr.com/index.json`.
To pull `Whisparr.*` packages from it while everything else still comes from nuget.org, add the
feed and a package source mapping to your `nuget.config`:

```xml
<packageSources>
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  <add key="whisparr" value="https://nuget.whisparr.com/index.json" />
</packageSources>
<packageSourceMapping>
  <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  <packageSource key="whisparr"><package pattern="Whisparr.*" /></packageSource>
</packageSourceMapping>
```

## Matching stash exactly

The phash is a line-for-line port of the Go code stash runs (goimagehash v1.1.0 and
nfnt/resize). It's tested stage by stage (resize, grayscale, DCT, median) against
reference values from that Go code, and end to end against stash itself through
[peolic/videohashes](https://github.com/peolic/videohashes). See `tools/make-fixtures.sh`.

Two things decide whether two hashes of the same file agree:

- **Duration.** It decides which frames are sampled. Use the container duration
  (`format.duration`), rounded to 0.01 s. Stream or "best" runtimes shift the frames.
- **ffmpeg.** Different ffmpeg versions and CPU code paths can scale frames slightly
  differently. StashDB matches phashes by Hamming distance, so small differences still
  match. Exact equality is only guaranteed with the same ffmpeg build.

The reference values come from linux/amd64. On arm64, Go fuses multiply-adds, so stash's
own phash can differ there in the lowest bits of a few coefficients. This port never fuses,
so it gives the amd64 result on every platform.

## Supported runtimes

`linux-x64`, `linux-arm`, `linux-arm64`, `linux-musl-x64`, `linux-musl-arm64`, `osx-x64`,
`osx-arm64`, `win-x86`, `win-x64`, `win-arm64`, `freebsd-x64`, `freebsd-arm64`.

## License

GPL-3.0. Ported third-party code is listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
