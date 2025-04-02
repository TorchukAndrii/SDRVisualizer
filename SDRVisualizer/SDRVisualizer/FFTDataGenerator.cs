namespace SDRVisualizer;

public class FFTDataGenerator
{
    private readonly int[,] _data;
    private readonly Random _rand;
    private double _phaseShift;
    private int rowIndex;
    private int rows;
    private int cols;

    // Constructor that initializes the FFT data with default values.
    public FFTDataGenerator(int rows = 200, int cols = 1024)
    {
        this.rows = rows;
        this.cols = cols;
        _data = new int[rows, cols];
        _rand = new Random();
        _phaseShift = 0;
        rowIndex = 0;
        GenerateData();
    }
    
    private void GenerateData()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                _data[r, c] = _rand.Next(-65, -55); // Mostly steady data
            }
            
            // First 5 columns always max value
            for (int c = 0; c < 5; c++)
            {
                _data[r, c] = -20;
            }
            
            // Last 5 columns always min value
            for (int c = cols - 5; c < cols; c++)
            {
                _data[r, c] = -120;
            }
            
            // Introduce small spikes in every row
            int spikeCount = _rand.Next(3, 6); // Between 3 and 5 spikes per row
            for (int i = 0; i < spikeCount; i++)
            {
                int spikeCol = _rand.Next(5, cols - 5); // Choose a random column for the spike, avoiding fixed min/max zones
                _data[r, spikeCol] = _rand.Next(-80, -50); // Generate a smaller spike
            }
        }
    }

    public int[] GetRow()
    {
        var row = new int[_data.GetLength(1)];
        for (var i = 0; i < _data.GetLength(1); i++) row[i] = _data[rowIndex, i];

        rowIndex++;
        if (rowIndex == 200) rowIndex = 0;
        return row;
    }

    public void ResetData()
    {
        GenerateData();
    }
}