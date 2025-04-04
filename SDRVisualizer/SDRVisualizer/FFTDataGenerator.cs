using System;

namespace SDRVisualizer
{
    public class FFTDataGenerator
    {
        private readonly int[,] _data;
        private readonly Random _rand;
        private readonly int cols;
        private readonly int rows;
        private int rowIndex;
        private readonly double noiseLevel;  // Control the background noise level

        public FFTDataGenerator(int rows = 200, int cols = 1024, double noiseLevel = 0.2)
        {
            this.rows = rows;
            this.cols = cols;
            this.noiseLevel = noiseLevel;
            _data = new int[rows, cols];
            _rand = new Random();
            rowIndex = 0;
            GenerateData();
        }

        private void GenerateData(bool showBorders = false, bool addSpikes = false)
        {
            // Simulate background signal with sinusoidal and Gaussian noise
            var spikeCount = 3;
            int minValue = -120;
            int maxValue = -20;

            for (var r = 0; r < rows; r++)
            {
                // Generate background signal
                for (var c = 0; c < cols; c++)
                {
                    // Simulate a sine wave at different frequencies
                    double frequency = c / (double)cols;  // Frequency is proportional to column index
                    double sineAmplitude = Math.Sin(2 * Math.PI * frequency * r / 100);  // Vary amplitude over time (rows)

                    // Map the sine amplitude from [-1, 1] to [-120, -20]
                    double amplitude = sineAmplitude * 50 - 70;  // Maps sine to range [-120, -20]

                    // Add some Gaussian noise to simulate FFT data

                    double noise = _rand.NextDouble() * noiseLevel - (noiseLevel / 2);  // Random noise between -noiseLevel/2 to +noiseLevel/2
                    amplitude += noise * 30;  // Scaling noise level so that it affects the result more significantly

                    // Clip the value to stay within the range [-120, -20]
                    _data[r, c] = (int)Math.Clamp(amplitude, minValue, maxValue);
                }

                if (showBorders)
                {
                    // First 5 columns always max value (high energy at lower frequencies)
                    for (var c = 0; c < 5; c++) _data[r, c] = -20;

                    // Last 5 columns always min value (high attenuation at higher frequencies)
                    for (var c = cols - 5; c < cols; c++) _data[r, c] = -120;
                }

                if (addSpikes)
                {
                    // Simulate occasional spikes
                    if (r % 20 == 0 && r != 0 && r % 100 != 0)
                        for (var i = 0; i < spikeCount; i++)
                        {
                            var spikeCol = _rand.Next(15, cols - 15);
                            for (var spikeRow = r; spikeRow > r - 10; spikeRow--)
                                _data[spikeRow, spikeCol] = _rand.Next(-40, -20);  // Small spike values
                        }
                    else if (r != 0 && r % 100 == 0) // Add bigger spikes every 100th row
                        for (var i = 0; i < spikeCount; i++)
                        {
                            var spikeCol = _rand.Next(70, cols - 70);
                            for (var spikeRow = r; spikeRow > r - 20; spikeRow--)
                                _data[spikeRow, spikeCol] = _rand.Next(-60, -20);  // Bigger spike values
                        }
                }
            }
        }

        public int[] GetRow()
        {
            var row = new int[_data.GetLength(1)];
            for (var i = 0; i < _data.GetLength(1); i++) row[i] = _data[rowIndex, i];

            rowIndex++;
            if (rowIndex == rows) rowIndex = 0;
            return row;
        }

        public void ResetData()
        {
            GenerateData();
        }
    }
}
