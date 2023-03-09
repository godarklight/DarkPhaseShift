#!/bin/sh
gnuplot << EOF
 set datafile separator ','
 set term pngcairo size 1024,1024
 set output 'plot1.png'
 set grid
 set xrange [0:512]
 set yrange [-0.5:0.5]
 plot 'test.csv' using 1:2 title 'Real' with lines, 'test.csv' using 1:3 title 'Imaginary' with lines
EOF
