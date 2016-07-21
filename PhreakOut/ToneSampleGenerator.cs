using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhreakOut
{
    class ToneSampleGenerator
    {
        /// <summary>
        /// Volume (0-1.0) of the preoduced tones
        /// </summary>
        public double Volume { get; set; }
        /// <summary>
        /// Sample rate (in Samples/sec) 
        /// </summary>
        public uint SampleRate { get { return mSampleRate; } set { mSampleRate = value; pRecalculateThetas(); } }
        private uint mSampleRate;

        /* Our state values. */
        /// <summary>
        /// Tones generated (private)
        /// </summary>
        private int[] mTones = { };
        /// <summary>
        /// θ/sample (at sample rate)
        /// </summary>
        private double[] mThetaPerSample = { };
        /// <summary>
        /// Current θ angles 
        /// </summary>
        private double[] mThetas = { };

        /// <summary>
        /// Tones to generate (in Hz)
        /// </summary>
        public int[] Tones { get { return mTones; } set { mTones = value; pRecalculateThetas(); } }

        /// <summary>
        /// Recalculate the θ/sample values for the input.
        /// </summary>
        private void pRecalculateThetas()
        {
            lock (mTones)
            {
                // Recalculate the thetas. 
                mThetaPerSample = new double[mTones.Length];
                mThetas = new double[mTones.Length];

                for (int iTone = 0; iTone < mTones.Length; iTone++)
                {
                    mThetas[iTone] = 0.00;
                    mThetaPerSample[iTone] = ((float)(mTones[iTone]) * (2 * Math.PI)) / ((double)(SampleRate));
                }
            }
        }

        /// <summary>
        /// Generate samples of the current tones.
        /// </summary>
        /// <param name="nSamples">Number of samples to generate</param>
        /// <returns>a <see cref="float"/> array containing <paramref name="nSamples"/> samples at <see cref="ToneSampleGenerator.SampleRate"/>.</returns>
        public float[] getSamples(uint nSamples)
        {
            // get some samples
            float[] content = new float[nSamples];
            lock (mTones)
            {

                // If there are no tones to generate, return an array full of emptiness.
                if (mTones == null || mTones.Length < 1)
                {
                    System.Diagnostics.Debug.WriteLine("!!! was given 0 tones. WTF? !!!");
                    // There's nothing for us to generate.
                    // return a whole bunch of 0's
                    for (int i = 0; i < nSamples; i++) content[i] = 0.00f;
                    return content;
                }

                // We actually sould be able to generate tones. 
                for (int i = 0; i < nSamples; i++)
                {
                    // get a sample;
                    double sum = 0;
                    for (int iTone = 0; iTone < mTones.Length; iTone++)
                    {
                        sum += Math.Sin(mThetas[iTone]);

                        mThetas[iTone] += mThetaPerSample[iTone];
                    }
                    sum = sum / ((float)(mThetas.Length));
                    content[i] = ((float)sum) * ((float)Volume);
                }
            }
            return content;
        }

    }
}
