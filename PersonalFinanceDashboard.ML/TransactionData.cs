using Microsoft.ML;
using Microsoft.ML.Data;
namespace PersonalFinanceDashboard.ML
{
    public class TransactionData
    {
        [LoadColumn(0)]
        public string Description { get; set; }

        [LoadColumn(1)]
        public string Category { get; set; }
    }

    public class CategoryPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedCategory { get; set; }
    }
}
