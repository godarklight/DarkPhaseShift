using System;
using System.Numerics;

namespace DarkPhaseShift.Common
{
    public static class FormatConvert
    {
        public static void S16ToDouble(byte[] input, double[] output)
        {
            for (int i = 0; i < output.Length; i++)
            {
                short sample = (short)((input[i * 2 + 1] << 8) | input[i * 2]);
                output[i] = sample / (double)short.MaxValue;
            }
        }
        public static void DoubleToS16(double[] input, byte[] output)
        {
            for (int i = 0; i < input.Length; i++)
            {
                short sample = (short)(input[i] * short.MaxValue);
                output[i * 2 + 1] = (byte)(sample >> 8);
                output[i * 2] = (byte)(sample & 255);
            }
        }

        public static void ComplexToS16(Complex[] input, byte[] output)
        {
            for (int i = 0; i < input.Length; i++)
            {
                short isample = (short)(input[i].Real * short.MaxValue);
                output[i * 4 + 1] = (byte)(isample >> 8);
                output[i * 4] = (byte)(isample & 255);
                short qsample = (short)(input[i].Imaginary * short.MaxValue);
                output[i * 4 + 3] = (byte)(qsample >> 8);
                output[i * 4 + 2] = (byte)(qsample & 255);
            }
        }

        public static void ComplexToIQData(Complex[] input, byte[] output)
        {
            for (int i = 0; i < input.Length; i++)
            {
                double iSample = Math.Clamp(input[i].Real, -1, 1);
                double qSample = Math.Clamp(input[i].Imaginary, -1, 1);
                byte iByte = (byte)(iSample * 127 + 128);
                byte qByte = (byte)(qSample * 127 + 128);
                output[6 + i * 2] = iByte;
                output[6 + i * 2 + 1] = qByte;
            }
        }

        /*
        public static double S16ToDouble(byte[] input, int index)
        {
            short rawShort = (short)(input[index + 1] << 8);
            rawShort |= (short)(input[index]);
            return rawShort / (double)short.MaxValue;
        }
        public static void DoubleToS16(double input, byte[] output, int index)
        {
            short rawShort = (short)(input * short.MaxValue);
            output[index] = (byte)(rawShort & 0xFF);
            output[index + 1] = (byte)(rawShort >> 8);
        }
        public static void IQToByteArray8(Complex input, byte[] output, int index)
        {
            output[index] = (byte)((input.Real + 1.0) * 127);
            output[index + 1] = (byte)((input.Imaginary + 1.0) * 127);
        }
        public static Complex ByteArrayToIQ8(byte[] input, int index)
        {
            double realPart = (input[index] / 127.0) - 1.0;
            double imaginaryPart = (input[index + 1] / 127.0) - 1.0;
            return new Complex(realPart, imaginaryPart);
        }
        public static void IQToByteArray16(Complex input, byte[] output, int index)
        {
            DoubleToS16(input.Real, output, index);
            DoubleToS16(input.Imaginary, output, index + 2);
        }
        public static Complex ByteArrayToIQ16(byte[] input, int index)
        {
            return new Complex(S16ToDouble(input, index), S16ToDouble(input, index + 2));
        }
        */
    }
}