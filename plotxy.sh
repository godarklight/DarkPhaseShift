#!/bin/sh
gnuplot << EOF
 set datafile separator ','
 set output 'xy.png'
 set grid
 set term pngcairo size 1024,1024
 set xrange [-0.3:0.3]
 set yrange [-0.3:0.3]
 plot 'test.csv' using 2:3 title 'XY' with lines
EOF
