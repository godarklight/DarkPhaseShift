#!/bin/sh
gnuplot --persist << EOF
 set datafile separator ','
 set term wxt
 plot 'test.csv' using 1:2 title 'Real' with lines, 'test.csv' using 1:3 title 'Imaginary' with lines, 'test2.csv' using (\$1+4096):2 title 'Real2' with lines, 'test2.csv' using (\$1+4096):3 title 'Imaginary2' with lines
EOF
