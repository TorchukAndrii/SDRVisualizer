namespace SDRVisualizer;

public class FFTDataGenerator
{
    private readonly int[,] _data;
    private readonly Random _rand;
    private readonly int cols;
    private readonly int rows;
    private int rowIndex;
    
    public FFTDataGenerator(int rows = 200, int cols = 1024)
    {
        this.rows = rows;
        this.cols = cols;
        _data = new int[rows, cols];
        _rand = new Random();
        rowIndex = 0;
        GenerateData();
    }

    private void GenerateData()
    {
        var spikeCount = 3;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++) _data[r, c] = _rand.Next(-100, -90); // Mostly steady data

            // First 5 columns always max value
            for (var c = 0; c < 5; c++) _data[r, c] = -20;

            // Last 5 columns always min value
            for (var c = cols - 5; c < cols; c++) _data[r, c] = -120;

            // Add small spikes
            if (r % 20 == 0 && r != 0 && r % 100 != 0)
                for (var i = 0; i < spikeCount; i++)
                {
                    var spikeCol = _rand.Next(15, cols - 15);
                    for (var spikeRow = r; spikeRow > r - 10; spikeRow--)
                    for (var j = 0; j < 5; j++)
                        if (r % 40 == 0)
                        {
                            _data[spikeRow, spikeCol + j] = _rand.Next(-120, -100);
                            _data[spikeRow, spikeCol - j] = _rand.Next(-120, -100);
                        }
                        else
                        {
                            _data[spikeRow, spikeCol + j] = _rand.Next(-40, -20);
                            _data[spikeRow, spikeCol - j] = _rand.Next(-40, -20);
                        }
                }
            else if (r != 0 && r % 100 == 0) // Add big spikes
                for (var i = 0; i < spikeCount; i++)
                {
                    var spikeCol = _rand.Next(70, cols - 70);
                    for (var spikeRow = r; spikeRow > r - 20; spikeRow--)
                    for (var j = 0; j < 30; j++)
                    {
                        _data[spikeRow, spikeCol + j] = _rand.Next(-110 + j, -50 - j);
                        _data[spikeRow, spikeCol - j] = _rand.Next(-110 + j, -50 - j);
                    }
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