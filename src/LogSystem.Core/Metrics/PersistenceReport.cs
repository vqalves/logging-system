namespace LogSystem.Core.Metrics;

public class PersistenceReport
{
    public PersistenceReport()
    {
        
    }
    
    /*
    new
    {
        ReadingFromChannel = TimeSpan.Zero,
        GroupingByCollectionName = TimeSpan.Zero,
        MessageCount = 0,
        TotalExecutionTime = TimeSpan.Zero,

        Batches = new[]
        {
            new
            {
                MessageCount = 0,
                RetrieveLogCollection = TimeSpan.Zero,
                UpdateLogForFileData = TimeSpan.Zero,
                TotalExecutionTime = TimeSpan.Zero,

                Azure = new
                {
                    CreateJsonContent = TimeSpan.Zero,
                    ConnectToAzure = TimeSpan.Zero,
                    CompressToGzip = TimeSpan.Zero,
                    UploadFile = TimeSpan.Zero,
                    TotalExecutionTime = TimeSpan.Zero,
                },

                Database = new
                {
                    OpenConnectionToDatabase = TimeSpan.Zero,
                    SaveData = TimeSpan.Zero,
                    TotalExecutionTime = TimeSpan.Zero
                }
            }
        }
    };
    */
}
