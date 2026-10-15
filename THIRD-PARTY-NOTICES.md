# Third-party notices

The C# phash in this package is a port of the Go code stash uses, so it produces the
same hashes. The ported parts and their licenses:

| Ported from | Into | License |
|---|---|---|
| [corona10/goimagehash](https://github.com/corona10/goimagehash) v1.1.0 — `PerceptionHash`, `transforms` (DCT, grayscale), `etcs` (median) | `Internal/PerceptionHash.cs`, `Internal/Dct.cs`, `Internal/DctTables.cs` | BSD-2-Clause |
| [nfnt/resize](https://github.com/nfnt/resize) `83c6a99` — bilinear `Resize`, `createWeights8`, `resizeNRGBA`/`resizeRGBA` | `Internal/BilinearResize.cs` | ISC |

The frame sampling and 5x5 sprite layout follow stash's
[`pkg/hash/videophash`](https://github.com/stashapp/stash/tree/develop/pkg/hash/videophash)
and the hash format follows `pkg/utils/phash.go`. No stash source is included; only
its parameters are reproduced.

The ffmpeg binaries bundled in the package are built from
[FFmpeg](https://ffmpeg.org) source by `build/ffmpeg/`, configured `--enable-version3`
(GPL-3.0). See `build/ffmpeg/README.md` for the exact configuration and source version.

## goimagehash

```
BSD 2-Clause License

Copyright (c) 2017, Dong-hee Na
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

## nfnt/resize

```
Copyright (c) 2012, Jan Schlicht <jan.schlicht@gmail.com>

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
```
