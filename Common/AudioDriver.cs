using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.Numerics;
using System.IO;

namespace DarkPhaseShift.Common
{
    public class AudioDriver
    {
        public bool running = true;
        Process audioInProcess;
        Process audioOutProcess;
        Thread audioThread;
        private bool debug = false;
        //Trigger debug after 1 seconds
        private int trigger = 48000 / AUDIO_SAMPLES_PER_CHUNK;
        const int AUDIO_SAMPLES_PER_CHUNK = 4096;
        int numLowOutput = 0;
        bool startAudio = false;
        bool isFile = false;
        Stream inputStream;
        Stream outputStream;
        Thread outputThread;
        ConcurrentQueue<Complex[]> outputQueue = new ConcurrentQueue<Complex[]>();

        double[] rolloffLow;
        int rolloffLowIndex;
        double[] rolloffHigh;
        int rolloffHighIndex;

        public AudioDriver(bool startAudio, string fileName, bool debug)
        {
            InitRolloff();
            this.startAudio = startAudio;
            if (startAudio)
            {
                ProcessStartInfo psi = new ProcessStartInfo("pw-record", "--target default --rate 48000 --channels 1 -");
                ProcessStartInfo psi2 = new ProcessStartInfo("pw-play", "--target default --rate 48000 --channels 2 -");
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi2.RedirectStandardInput = true;
                psi2.RedirectStandardOutput = true;
                audioInProcess = Process.Start(psi);
                audioOutProcess = Process.Start(psi2);
                inputStream = audioInProcess.StandardOutput.BaseStream;
                outputStream = audioOutProcess.StandardInput.BaseStream;
            }
            else
            {
                if (fileName == null)
                {
                    inputStream = Console.OpenStandardInput();
                    outputStream = Console.OpenStandardOutput();
                }
                else
                {
                    isFile = true;
                    inputStream = new FileStream(fileName, FileMode.Open);
                    if (!debug)
                    {
                        string outName = fileName + "-out.raw";
                        outputStream = new FileStream(outName, FileMode.Create);
                    }
                    else
                    {
                        ProcessStartInfo psi2 = new ProcessStartInfo("pw-play", "--target default --rate 48000 --channels 2 -");
                        psi2.RedirectStandardInput = true;
                        audioOutProcess = Process.Start(psi2);
                        outputStream = audioOutProcess.StandardInput.BaseStream;
                    }
                }
            }
            //audioInProcess.Start();
            //audioOutProcess.Start();
            audioThread = new Thread(new ThreadStart(AudioLoop));
            audioThread.Start();
            outputThread = new Thread(new ThreadStart(OutputLoop));
            outputThread.Start();
        }

        private void InitRolloff()
        {
            //Roll off from 100 to 150
            rolloffLowIndex = (2 * AUDIO_SAMPLES_PER_CHUNK * 50) / 48000;
            int rolloffEndLow = (2 * AUDIO_SAMPLES_PER_CHUNK * 100) / 48000;
            rolloffLow = new double[rolloffEndLow - rolloffLowIndex];
            for (int i = 0; i < rolloffLow.Length; i++)
            {
                double position = 1.0 - (i / (double)rolloffLow.Length);
                double val = Math.Exp(-(position * position) * 10);
                rolloffLow[i] = val;
            }

            //Roll off from 2.8 to 3
            rolloffHighIndex = (2 * AUDIO_SAMPLES_PER_CHUNK * 2700) / 48000;
            int rolloffEndHigh = (2 * AUDIO_SAMPLES_PER_CHUNK * 3000) / 48000;
            rolloffHigh = new double[rolloffEndHigh - rolloffHighIndex];
            for (int i = 0; i < rolloffHigh.Length; i++)
            {
                double position = i / (double)rolloffHigh.Length;
                double val = Math.Exp(-(position * position) * 10);
                rolloffHigh[i] = val;
            }
        }

        private void AudioLoop()
        {

            long lastGenerateTime = DateTime.UtcNow.Ticks;
            //S16LE is two bytes per sample
            byte[] audioChunk = new byte[AUDIO_SAMPLES_PER_CHUNK * 2];
            int bufferPos = 0;
            while (running)
            {
                int readBytes = 0;
                if (isFile)
                {
                    int fileLeft = (int)(inputStream.Length - inputStream.Position);
                    int bytesToRead = fileLeft;
                    if (bytesToRead > audioChunk.Length)
                    {
                        bytesToRead = audioChunk.Length;
                    }
                    //Pad the end of the file with 0's to get a multiple of the FFT.
                    if (bytesToRead < audioChunk.Length)
                    {
                        Array.Clear(audioChunk);
                        running = false;
                    }
                    readBytes = inputStream.Read(audioChunk, bufferPos, audioChunk.Length - bufferPos);
                    bufferPos = audioChunk.Length;
                }
                else
                {
                    readBytes = inputStream.Read(audioChunk, bufferPos, audioChunk.Length - bufferPos);
                    bufferPos += readBytes;
                }
                if (bufferPos == audioChunk.Length)
                {
                    bufferPos = 0;
                    ProcessChunk(audioChunk);
                }
            }
            if (audioInProcess != null)
            {
                audioInProcess.Kill();
            }
            if (audioOutProcess != null)
            {
                audioOutProcess.Kill();
            }

            outputThread.Join();
        }



        //Input is two channel, so output must be twice as big
        byte[] outputChunk = new byte[4 * AUDIO_SAMPLES_PER_CHUNK];
        double[] inputDouble = new double[AUDIO_SAMPLES_PER_CHUNK];
        //We are only using the middle 50% of the FFT
        Complex[] inputComplex = new Complex[2 * AUDIO_SAMPLES_PER_CHUNK];
        Complex[] outputComplex = new Complex[AUDIO_SAMPLES_PER_CHUNK];
        Complex[] lastIFFT = new Complex[2 * AUDIO_SAMPLES_PER_CHUNK];
        private void ProcessChunk(byte[] input)
        {
            //Copy data to the start of the array
            Array.Copy(inputComplex, inputComplex.Length / 2, inputComplex, 0, inputComplex.Length / 2);

            //Convert S16 byte[] to double[]
            FormatConvert.S16ToDouble(input, inputDouble);

            //Convert two double[] to complex[]
            for (int i = 0; i < inputDouble.Length; i++)
            {
                inputComplex[i + inputDouble.Length] = inputDouble[i];
            }

            //Convert time domain to frequency domain
            Complex[] fft = FFT.CalcFFT(inputComplex);

            //Hilbert transform, leave DC and nyquist alone, double positive frequencies and wipe negative frequencies
            for (int i = 0; i < fft.Length / 2; i++)
            {
                fft[i] = fft[i] * 2;
            }
            for (int i = (fft.Length / 2) + 1; i < fft.Length; i++)
            {
                fft[i] = Complex.Zero;
            }

            //Filter everything below the low filter, leave DC
            for (int i = 1; i < rolloffLowIndex - 1; i++)
            {
                fft[i] = Complex.Zero;
            }

            //Low filter
            for (int i = 0; i < rolloffLow.Length; i++)
            {
                fft[i + rolloffLowIndex] = fft[i + rolloffLowIndex] * rolloffLow[i];
            }

            //High filter
            for (int i = 0; i < rolloffHigh.Length; i++)
            {
                fft[i + rolloffHighIndex] = fft[i + rolloffHighIndex] * rolloffHigh[i];
            }

            for (int i = rolloffHighIndex + rolloffHigh.Length; i < fft.Length; i++)
            {
                if (i != fft.Length / 2)
                {
                    fft[i] = Complex.Zero;
                }
            }


            outputQueue.Enqueue(fft);
        }

        private void OutputLoop()
        {
            while (running)
            {
                if (outputQueue.TryDequeue(out Complex[] fft))
                {
                    //Convert from frequency domain to time domain
                    Complex[] ifft = FFT.CalcIFFT(fft);

                    //Interpolate between ffts.
                    for (int i = 0; i < inputDouble.Length; i++)
                    {
                        double scaleValue = i / (double)inputDouble.Length;
                        double invScaleValue = 1d - scaleValue;
                        int newIndex = i;
                        int oldIndex = newIndex + AUDIO_SAMPLES_PER_CHUNK;
                        outputComplex[i] = scaleValue * ifft[newIndex] + invScaleValue * lastIFFT[oldIndex];
                    }
                    lastIFFT = ifft;

                    //Reduce Right channel by 1.5db because of balance issues
                    for (int i = 0; i < outputComplex.Length; i++)
                    {
                        Complex current = outputComplex[i];
                        Complex adjusted = new Complex(current.Imaginary * 0.75, current.Real);
                        outputComplex[i] = adjusted;
                    }

                    //Convert from IQ samples to 2 channel S16
                    FormatConvert.ComplexToS16(outputComplex, outputChunk);

                    //Debug plots
                    if (debug)
                    {
                        trigger--;
                        if (trigger == 0)
                        {
                            File.Delete("test.csv");
                            using (StreamWriter sw = new StreamWriter("test.csv"))
                            {
                                int sample = 0;
                                foreach (Complex c in outputComplex)
                                {
                                    sw.WriteLine($"{sample},{c.Real},{c.Imaginary}");
                                    sample++;
                                }
                            }
                        }
                        if (trigger == -1)
                        {
                            File.Delete("test2.csv");
                            using (StreamWriter sw = new StreamWriter("test2.csv"))
                            {
                                int sample = 0;
                                foreach (Complex c in outputComplex)
                                {
                                    sw.WriteLine($"{sample},{c.Real},{c.Imaginary}");
                                    sample++;
                                }
                            }
                            Console.WriteLine("Break");
                        }
                    }

                    //Let the stream go inactive when there is no data so the program can resync
                    double maxVal = 0;
                    for (int i = 0; i < outputComplex.Length; i++)
                    {
                        if (outputComplex[i].Real > maxVal)
                        {
                            maxVal = outputComplex[i].Real;
                        }
                    }

                    //-40dbFS
                    //Console.WriteLine($"ACTIVE: {(maxVal > 0.0001d)}");

                    if (!isFile && maxVal < 0.0001d)
                    {
                        numLowOutput++;
                    }
                    else
                    {
                        numLowOutput = 0;
                    }

                    if ((running || isFile) && numLowOutput < 20)
                    {
                        outputStream.Write(outputChunk, 0, outputChunk.Length);
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public void Stop()
        {
            running = false;
            audioThread.Join();
        }
    }
}
