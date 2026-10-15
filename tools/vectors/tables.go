// Code copied verbatim from github.com/corona10/goimagehash@v1.1.0 transforms/static.go
// (BSD-2-Clause, Copyright 2017 The goimagehash Authors) so the table literals are
// constant-folded exactly as upstream. Do not reformat the expressions.

package main

import "math"

// Static DCT Tables
var (
	dct64  = [32]float64{
		(math.Cos((float64(0)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(1)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(2)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(3)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(4)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(5)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(6)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(7)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(8)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(9)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(10)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(11)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(12)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(13)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(14)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(15)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(16)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(17)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(18)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(19)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(20)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(21)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(22)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(23)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(24)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(25)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(26)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(27)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(28)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(29)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(30)+0.5)*math.Pi/64) * 2),
		(math.Cos((float64(31)+0.5)*math.Pi/64) * 2),
	}
	dct32 = [16]float64{
		(math.Cos((float64(0)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(1)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(2)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(3)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(4)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(5)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(6)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(7)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(8)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(9)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(10)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(11)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(12)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(13)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(14)+0.5)*math.Pi/32) * 2),
		(math.Cos((float64(15)+0.5)*math.Pi/32) * 2),
	}
	dct16 = [8]float64{
		(math.Cos((float64(0)+0.5)*math.Pi/16) * 2),
		(math.Cos((float64(1)+0.5)*math.Pi/16) * 2),
		(math.Cos((float64(2)+0.5)*math.Pi/16) * 2),
		(math.Cos((float64(3)+0.5)*math.Pi/16) * 2),
		(math.Cos((float64(4)+0.5)*math.Pi/16) * 2),
		(math.Cos((float64(5)+0.5)*math.Pi/16) * 2),
		(math.Cos((float64(6)+0.5)*math.Pi/16) * 2),
		(math.Cos((float64(7)+0.5)*math.Pi/16) * 2),
	}
	dct8 = [4]float64{
		(math.Cos((float64(0)+0.5)*math.Pi/8) * 2),
		(math.Cos((float64(1)+0.5)*math.Pi/8) * 2),
		(math.Cos((float64(2)+0.5)*math.Pi/8) * 2),
		(math.Cos((float64(3)+0.5)*math.Pi/8) * 2),
	}
	dct4 = [2]float64{
		(math.Cos((float64(0)+0.5)*math.Pi/4) * 2),
		(math.Cos((float64(1)+0.5)*math.Pi/4) * 2),
	}
	dct2 = [1]float64{
		(math.Cos((float64(0)+0.5)*math.Pi/2) * 2),
	}
)
