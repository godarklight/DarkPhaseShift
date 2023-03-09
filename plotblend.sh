#!/bin/sh
gnuplot --persist << EOF
 set datafile separator ','
 set term wxt
 plot 'test.csv' using 1:2 title 'Real' with lines, 'test2.csv' using (\$1+4096):2 title 'Real2' with lines, 'blend.csv' using (\$1+4096):2 title 'Sig1' with points, 'blend.csv' using (\$1+4096):3 title 'Sig2' with points, 'blend.csv' using (\$1+4096):4 title 'GEN' with lines
EOF
