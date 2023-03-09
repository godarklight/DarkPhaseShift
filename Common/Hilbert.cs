//https://flylib.com/books/en/2.729.1/designing_a_discrete_hilbert_transformer.html 9.4.2
using System;
using System.Numerics;

namespace DarkPhaseShift.Common
{
    public static class Hilbert
    {
        public static Complex[] Calculate(Complex[] input)
        {
            Complex[] fft = FFT.CalcFFT(input);
            Complex[] retVal = new Complex[fft.Length];
            for (int i = 0; i < fft.Length / 2; i++)
            {
                //Multiply FFT by 2
                retVal[i] = 2.0 * fft[i];
            }

            //Divide DC term and half term by two
            retVal[0] = retVal[0] / 2;
            retVal[fft.Length / 2] = retVal[fft.Length / 2];

            return (FFT.CalcIFFT(retVal));
        }

        public static Complex[] Calculate(double[] samples)
        {
            Complex[] temp = new Complex[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                temp[i] = samples[i];
            }
            return Calculate(temp);
        }
    }
}
