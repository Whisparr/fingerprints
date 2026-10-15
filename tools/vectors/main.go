// Command vectors dumps reference values from the Go implementations that stash's
// phash depends on (goimagehash v1.1.0, nfnt/resize, x/image/bmp, imaging), so the
// C# port can be tested stage by stage against them.
//
//	vectors tables                      DCT tables as IEEE-754 bit patterns
//	vectors synthetic                   every pipeline stage for a deterministic sprite
//	vectors seeks <duration>...         ffmpeg -ss strings for the 25 sprite frames
//	vectors video <ffmpeg> <file> <dur> full pipeline against a real video
package main

import (
	"bytes"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"image"
	"image/color"
	"math"
	"os"
	"os/exec"
	"strconv"

	"github.com/corona10/goimagehash"
	"github.com/corona10/goimagehash/transforms"
	"github.com/disintegration/imaging"
	"github.com/nfnt/resize"
	_ "golang.org/x/image/bmp"
)

const (
	screenshotSize = 160
	columns        = 5
	rows           = 5
)

func main() {
	if len(os.Args) < 2 {
		fail("usage: vectors tables|synthetic|seeks|video ...")
	}

	var out any
	switch os.Args[1] {
	case "tables":
		out = tableBits()
	case "synthetic":
		out = stages(syntheticSprite())
	case "seeks":
		out = seeks(os.Args[2:])
	case "video":
		if len(os.Args) != 5 {
			fail("usage: vectors video <ffmpeg> <file> <duration>")
		}
		out = video(os.Args[2], os.Args[3], os.Args[4])
	case "frames":
		if len(os.Args) != 3 {
			fail("usage: vectors frames <dir with 00.bmp..24.bmp>")
		}
		out = frames(os.Args[2])
	default:
		fail("unknown mode " + os.Args[1])
	}

	enc := json.NewEncoder(os.Stdout)
	enc.SetIndent("", "  ")
	if err := enc.Encode(out); err != nil {
		fail(err.Error())
	}
}

func fail(msg string) {
	fmt.Fprintln(os.Stderr, msg)
	os.Exit(1)
}

func bits(values []float64) []string {
	out := make([]string, len(values))
	for i, v := range values {
		out[i] = fmt.Sprintf("%016x", math.Float64bits(v))
	}
	return out
}

func tableBits() map[string][]string {
	return map[string][]string{
		"dct64": bits(dct64[:]),
		"dct32": bits(dct32[:]),
		"dct16": bits(dct16[:]),
		"dct8":  bits(dct8[:]),
		"dct4":  bits(dct4[:]),
		"dct2":  bits(dct2[:]),
	}
}

// syntheticSprite builds an 800x450 opaque NRGBA image from a formula the C# tests
// reproduce, standing in for a real 5x5 montage of 160x90 frames.
func syntheticSprite() *image.NRGBA {
	img := image.NewNRGBA(image.Rect(0, 0, 800, 450))
	for y := 0; y < 450; y++ {
		for x := 0; x < 800; x++ {
			img.SetNRGBA(x, y, color.NRGBA{
				R: uint8((x*7 + y*3) % 256),
				G: uint8((x*x + y*5) % 256),
				B: uint8((x ^ y) % 256),
				A: 255,
			})
		}
	}
	return img
}

type stageDump struct {
	SpriteWidth   int      `json:"spriteWidth"`
	SpriteHeight  int      `json:"spriteHeight"`
	SpriteSha256  string   `json:"spriteSha256"`
	ResizedRGBA   string   `json:"resizedRgbaHex"`
	Gray          []string `json:"grayBits"`
	Dct           []string `json:"dctBits"`
	Flattened     []string `json:"flattenedBits"`
	Median        string   `json:"medianBits"`
	PHash         string   `json:"phash"`
	PHashFromLib  string   `json:"phashFromGoimagehash"`
}

// stages mirrors goimagehash.PerceptionHash step by step, then checks the result
// against the library call itself so the dump can't drift from upstream.
func stages(sprite *image.NRGBA) stageDump {
	sum := sha256.Sum256(sprite.Pix)

	resized := resize.Resize(64, 64, sprite, resize.Bilinear).(*image.RGBA)

	pixels := make([]float64, 4096)
	transforms.Rgb2GrayFast(resized, &pixels)
	gray := append([]float64(nil), pixels...)

	transforms.DCT2DFast64(&pixels)
	dct := append([]float64(nil), pixels...)

	flattens := transforms.FlattenPixelsFast64(pixels, 8, 8)
	flat := append([]float64(nil), flattens...)

	median := medianOfPixelsFast64(flattens)

	var hash uint64
	for idx, p := range flattens {
		if p > median {
			hash |= 1 << uint(64-idx-1)
		}
	}

	lib, err := goimagehash.PerceptionHash(sprite)
	if err != nil {
		fail(err.Error())
	}

	return stageDump{
		SpriteWidth:  sprite.Bounds().Dx(),
		SpriteHeight: sprite.Bounds().Dy(),
		SpriteSha256: hex.EncodeToString(sum[:]),
		ResizedRGBA:  hex.EncodeToString(resized.Pix),
		Gray:         bits(gray),
		Dct:          bits(dct),
		Flattened:    bits(flat),
		Median:       fmt.Sprintf("%016x", math.Float64bits(median)),
		PHash:        strconv.FormatUint(hash, 16),
		PHashFromLib: strconv.FormatUint(lib.GetHash(), 16),
	}
}

// medianOfPixelsFast64 is goimagehash etcs.MedianOfPixelsFast64, which the
// package exports; called through here only to keep the copy of flattens intact.
func medianOfPixelsFast64(pixels []float64) float64 {
	tmp := [64]float64{}
	copy(tmp[:], pixels)
	return quickSelectMedian(tmp[:], 0, len(tmp)-1, len(tmp)/2)
}

func quickSelectMedian(sequence []float64, low int, hi int, k int) float64 {
	if low == hi {
		return sequence[k]
	}

	for low < hi {
		pivot := low/2 + hi/2
		pivotValue := sequence[pivot]
		storeIdx := low
		sequence[pivot], sequence[hi] = sequence[hi], sequence[pivot]
		for i := low; i < hi; i++ {
			if sequence[i] < pivotValue {
				sequence[storeIdx], sequence[i] = sequence[i], sequence[storeIdx]
				storeIdx++
			}
		}
		sequence[hi], sequence[storeIdx] = sequence[storeIdx], sequence[hi]
		if k <= storeIdx {
			hi = storeIdx
		} else {
			low = storeIdx + 1
		}
	}

	if len(sequence)%2 == 0 {
		return sequence[k-1]/2 + sequence[k]/2
	}
	return sequence[k]
}

func frameTimes(duration float64) []float64 {
	chunkCount := columns * rows
	offset := 0.05 * duration
	stepSize := (0.9 * duration) / float64(chunkCount)
	times := make([]float64, chunkCount)
	for i := 0; i < chunkCount; i++ {
		times[i] = offset + (float64(i) * stepSize)
	}
	return times
}

func seeks(durations []string) map[string][]string {
	out := map[string][]string{}
	for _, d := range durations {
		duration, err := strconv.ParseFloat(d, 64)
		if err != nil {
			fail(err.Error())
		}
		var s []string
		for _, t := range frameTimes(duration) {
			s = append(s, fmt.Sprint(t))
		}
		out[d] = s
	}
	return out
}

func screenshot(ffmpeg, input string, t float64, slowSeek bool) (image.Image, error) {
	args := []string{"-v", "error", "-y"}
	if !slowSeek {
		args = append(args, "-ss", fmt.Sprint(t))
	}
	args = append(args, "-i", input)
	if slowSeek {
		args = append(args, "-ss", fmt.Sprint(t))
	}
	args = append(args, "-frames:v", "1", "-vf", fmt.Sprintf("scale=%d:-2", screenshotSize), "-c:v", "bmp", "-f", "rawvideo", "-")

	var stdout, stderr bytes.Buffer
	cmd := exec.Command(ffmpeg, args...)
	cmd.Stdout = &stdout
	cmd.Stderr = &stderr
	if err := cmd.Run(); err != nil {
		return nil, fmt.Errorf("%w: %s", err, stderr.String())
	}
	img, _, err := image.Decode(&stdout)
	return img, err
}

type videoDump struct {
	stageDump
	FrameWidth  int `json:"frameWidth"`
	FrameHeight int `json:"frameHeight"`
	SlowSeek    bool `json:"slowSeek"`
}

// video reproduces stash's generateSprite + combineImages, then dumps the stages.
func video(ffmpeg, file, durationArg string) videoDump {
	duration, err := strconv.ParseFloat(durationArg, 64)
	if err != nil {
		fail(err.Error())
	}

	var images []image.Image
	slowSeek := false
	for _, t := range frameTimes(duration) {
		img, err := screenshot(ffmpeg, file, t, slowSeek)
		if err != nil && !slowSeek {
			slowSeek = true
			img, err = screenshot(ffmpeg, file, t, slowSeek)
		}
		if err != nil {
			fail(err.Error())
		}
		images = append(images, img)
	}

	dump := montageDump(images)
	dump.SlowSeek = slowSeek
	return dump
}

// frames hashes 25 pre-extracted BMP screenshots, so a non-native Go build (e.g.
// amd64 under emulation) can hash frames produced by the host's ffmpeg.
func frames(dir string) videoDump {
	var images []image.Image
	for i := 0; i < columns*rows; i++ {
		f, err := os.Open(fmt.Sprintf("%s/%02d.bmp", dir, i))
		if err != nil {
			fail(err.Error())
		}
		img, _, err := image.Decode(f)
		f.Close()
		if err != nil {
			fail(err.Error())
		}
		images = append(images, img)
	}
	return montageDump(images)
}

// montageDump is stash's combineImages followed by the stage dump.
func montageDump(images []image.Image) videoDump {
	width := images[0].Bounds().Size().X
	height := images[0].Bounds().Size().Y
	montage := imaging.New(width*columns, height*rows, color.NRGBA{})
	for index := 0; index < len(images); index++ {
		x := width * (index % columns)
		y := height * int(math.Floor(float64(index)/float64(rows)))
		montage = imaging.Paste(montage, images[index], image.Pt(x, y))
	}

	return videoDump{
		stageDump:   stages(montage),
		FrameWidth:  width,
		FrameHeight: height,
	}
}
